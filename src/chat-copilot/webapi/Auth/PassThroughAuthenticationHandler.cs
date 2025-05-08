// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Services.Ctfd;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace CopilotChat.WebApi.Auth;

/// <summary>
/// Class implementing "authentication" that lets all requests pass through.
/// </summary>
public class PassThroughAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "PassThrough";

    public const string ClaimCtfdSessionId = "CtfdSessionId";
    public const string ClaimCtfdNonce = "CtfdNonce";
    public const string ClaimCtfdCookie = "CtfdCookie";

    public const string ContextReasonName = "Ctfd-Auth-Fail-Reason";

    private const string CtfdPrefixName = "flask_cache_session";
    private const string DefaultUserName = "Default User";

    private readonly ISessionMetricService _sessionMetric;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<ChallengeOptions> _challengeOptions;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly IItsDangerousSignerService? _itsDangerousSigner;

    /// <summary>
    /// Constructor
    /// </summary>
    public PassThroughAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        ISystemClock clock,
        ISessionMetricService sessionMetric,
        IMemoryCache memoryCache,
        IServiceProvider sp,
        IOptions<ChallengeOptions> challengeOptions) : base(options, loggerFactory, encoder, clock)
    {
        this._sessionMetric = sessionMetric;
        this._memoryCache = memoryCache;
        this._challengeOptions = challengeOptions;

        //Dynamically inject the connection multiplexer if the challenge is CTFD.
        if (challengeOptions.Value.AuthType == AuthType.CTFd)
        {
            var connectionMultiplexerService = sp.GetService(typeof(IConnectionMultiplexer));
            if (connectionMultiplexerService != null)
            {
                this._connectionMultiplexer = (IConnectionMultiplexer)connectionMultiplexerService;
            }
        }

        if (challengeOptions.Value.AuthType == AuthType.CTFd || challengeOptions.Value.AuthType == AuthType.ChallengeHome)
        {
            var itsDangerousSignerService = sp.GetService(typeof(IItsDangerousSignerService));
            if (itsDangerousSignerService != null)
            {
                this._itsDangerousSigner = (IItsDangerousSignerService)itsDangerousSignerService;
            }
        }
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        AuthenticateResult result;

        switch (this._challengeOptions.Value.AuthType)
        {
            case AuthType.None:
                result = this.HandleDefaultAuth();
                break;
            case AuthType.ChallengeHome:
                result = this.HandleChallengeHomeAuth();
                break;
            case AuthType.CTFd:
                result = await this.HandleCtfdAuthAsync();
                break;
            default:
                result = AuthenticateResult.Fail("Invalid auth type");
                break;
        }

        if (result.Succeeded)
        {
            var currentUserId = result.Principal.Claims.Where(c => c.Type == ClaimConstants.Sub).FirstOrDefault();
            if (currentUserId != null && !string.IsNullOrEmpty(currentUserId.Value))
            {
                this._sessionMetric.TrackUserId(currentUserId.Value);
            }
        }

        return result;
    }

    private AuthenticateResult HandleDefaultAuth()
    {
        this.Logger.LogInformation("Allowing request to pass through");

        string currentUserId;
        if (!this.Request.Cookies.ContainsKey("User-ID"))
        {
            currentUserId = this.SetCookie();
        }
        else
        {
            var cookieUserId = this.Request.Cookies["User-ID"];
            if (Guid.TryParse(cookieUserId, out Guid _))
            {
                currentUserId = cookieUserId;
            }
            else
            {
                currentUserId = this.SetCookie();
            }
        }

        Claim userIdClaim = new(ClaimConstants.Sub, currentUserId);
        Claim nameClaim = new(ClaimConstants.Name, DefaultUserName);
        ClaimsIdentity identity = new(new Claim[] { userIdClaim, nameClaim }, AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private AuthenticateResult HandleChallengeHomeAuth()
    {
        //Get the cookie and check if it's present in redis
        if (!this.Request.Cookies.ContainsKey("session"))
        {
            return this.AuthenticationFailure("Session cookie not found");
        }

        var sessionCookie = this.Request.Cookies["session"]!;

        //Split the session name and extract it
        var sessionCookieSplit = sessionCookie.Split(".");

        if (sessionCookieSplit.Length < 2)
        {
            return this.AuthenticationFailure("Invalid session cookie format");
        }

        var sessionDataB64UrlSafe = sessionCookieSplit[0];
        //Validate the signature
        if (this._itsDangerousSigner == null)
        {
            return this.AuthenticationFailure("CTFd cookie signer not available");
        }

        var signature = sessionCookieSplit[1];
        var validSignature = this._itsDangerousSigner.Sign(sessionDataB64UrlSafe);
        if (signature != validSignature)
        {
            return this.AuthenticationFailure("Invalid session cookie signature");
        }



        //Add the required padding to the base64 string
        sessionDataB64UrlSafe += new string('=', (4 - sessionDataB64UrlSafe.Length % 4) % 4);

        var json = Encoding.UTF8.GetString(Convert.FromBase64String(sessionDataB64UrlSafe
            .Replace("_", "/", StringComparison.InvariantCulture).Replace("-", "+", StringComparison.InvariantCulture)));

        //Extract the session data
        var jsonObject = JObject.Parse(json);
        if (jsonObject == null)
        {
            return this.AuthenticationFailure("Invalid session value");
        }

        //Extract the user id and nonce
        if (jsonObject["user_id"] == null || jsonObject["exp"] == null)
        {
            return this.AuthenticationFailure("Invalid session value. Missing required values");
        }

        var userId = jsonObject["user_id"]!.ToString();

        //Check if the user id is a valid guid
        if (!Guid.TryParse(userId, out Guid _))
        {
            return this.AuthenticationFailure("Invalid user id");
        }

        var exp = jsonObject["exp"]!.Value<long>();
        //Check if the session is expired
        if (exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return this.AuthenticationFailure("Session expired");
        }

        Claim userIdClaim = new(ClaimConstants.Sub, userId);
        Claim nameClaim = new(ClaimConstants.Name, DefaultUserName);
        ClaimsIdentity identity = new(new Claim[] { userIdClaim, nameClaim }, AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);
        AuthenticationTicket ticket = new(principal, this.Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<AuthenticateResult> HandleCtfdAuthAsync()
    {
        //Get the cookie and check if it's present in redis
        if (!this.Request.Cookies.ContainsKey("session"))
        {
            return this.AuthenticationFailure("Session cookie not found");
        }

        var sessionCookie = this.Request.Cookies["session"]!;

        //Check if the session cookie is in the memory cache before going to redis
        if (this._memoryCache.TryGetValue(this.MemoryKeyCookieKey(sessionCookie), out AuthenticationTicket ticket))
        {
            return AuthenticateResult.Success(ticket);
        }

        //Split the session name and extract it
        var sessionCookieSplit = sessionCookie.Split(".");

        if (sessionCookieSplit.Length < 2)
        {
            return this.AuthenticationFailure("Invalid session cookie format");
        }

        var sessionId = sessionCookieSplit[0];
        //Check if the sessionId is a valid guid
        if (!Guid.TryParse(sessionId, out Guid _))
        {
            return this.AuthenticationFailure("Invalid session cookie format");
        }

        //Validate the signature
        if (this._itsDangerousSigner == null)
        {
            return this.AuthenticationFailure("CTFd cookie signer not available");
        }

        var signature = sessionCookieSplit[1];
        var validSignature = this._itsDangerousSigner.Sign(sessionId);
        if (signature != validSignature)
        {
            return this.AuthenticationFailure("Invalid session cookie signature");
        }

        //Get the session from redis
        var keyName = $"{CtfdPrefixName}{sessionId}";
        var db = this._connectionMultiplexer!.GetDatabase(this._challengeOptions.Value.Ctfd!.RedisDatabase);
        string? sessionValue = await db.StringGetAsync(keyName);

        if (sessionValue == null)
        {
            return this.AuthenticationFailure("Session not found in redis");
        }

        //Parse the session value
        var jsonRegex = "{.+}";

        var match = Regex.Match(sessionValue, jsonRegex);
        if (!match.Success)
        {
            return this.AuthenticationFailure("Invalid session value regex error");
        }

        var json = match.Value;
        //Parse JSON

        var jsonObject = JObject.Parse(json);
        if (jsonObject == null)
        {
            return this.AuthenticationFailure("Invalid session value");
        }

        //Extract the user id and nonce
        if (jsonObject["id"] == null || jsonObject["nonce"] == null)
        {
            return this.AuthenticationFailure("Invalid session value. Missing required values");
        }
        var userId = Convert.ToInt32(jsonObject["id"], CultureInfo.InvariantCulture);

        var nonce = jsonObject["nonce"]!.ToString();

        var ctfdGuid = new CtfdGuid(userId);
        var currentUserId = ctfdGuid.ToString();

        Claim userIdClaim = new(ClaimConstants.Sub, currentUserId);
        Claim nameClaim = new(ClaimConstants.Name, DefaultUserName);
        Claim sessionClaim = new(ClaimCtfdSessionId, sessionId);
        Claim nonceClaim = new(ClaimCtfdNonce, nonce);
        Claim cookieClaim = new(ClaimCtfdCookie, sessionCookie);

        ClaimsIdentity identity = new(new Claim[] { userIdClaim, nameClaim, sessionClaim, nonceClaim, cookieClaim }, AuthenticationScheme);
        ClaimsPrincipal principal = new(identity);
        ticket = new(principal, this.Scheme.Name);

        //Cache the ticket
        this._memoryCache.Set(this.MemoryKeyCookieKey(sessionCookie), ticket, TimeSpan.FromMinutes(5));

        return AuthenticateResult.Success(ticket);
    }

    private AuthenticateResult AuthenticationFailure(string reason)
    {
        this.Context.Items[ContextReasonName] = reason;
        return AuthenticateResult.Fail(reason);
    }

    private string SetCookie()
    {
        var userId = Guid.NewGuid().ToString();
        this.Response.Cookies.Append("User-ID", userId);
        return userId;
    }

    private string MemoryKeyCookieKey(string cookie) => $"A_{cookie}";

    /// <summary>
    /// Returns true if the given user ID is the default user guest ID.
    /// </summary>
    public static bool IsDefaultUser(string userId) => true; //We don't have Azure AD auth enabled so always return true.
}
