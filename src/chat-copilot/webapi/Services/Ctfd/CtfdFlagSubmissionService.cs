// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Services.Ctfd;

public class CtfdFlagSubmissionService : ICtfdFlagSubmissionService
{
    private readonly ChallengeOptions _challengeOptions;
    private readonly bool _flagSubmissionEnabled;
    private readonly HttpClient? _httpClient;
    private readonly ILogger<CtfdFlagSubmissionService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHubContext<MessageRelayHub> _messageRelayHubContext;

    public CtfdFlagSubmissionService(
        IOptions<ChallengeOptions> challengeOptions,
        ILogger<CtfdFlagSubmissionService> logger,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IHubContext<MessageRelayHub> messageRelayHubContext
        )
    {
        this._challengeOptions = challengeOptions.Value;

        this._flagSubmissionEnabled = false;
        if (this._challengeOptions.Ctfd != null)
        {
            this._flagSubmissionEnabled = true;
            this._httpClient = httpClientFactory.CreateClient("Ctfd");
            this._httpClient.BaseAddress = this._challengeOptions.Ctfd.CtfdUrl;
        }

        this._logger = logger;
        this._httpContextAccessor = httpContextAccessor;
        this._messageRelayHubContext = messageRelayHubContext;
    }

    /// <summary>
    /// Get the CtfdAuthApi object if the flag submission is enabled for this challenge. This method can only be called in a valid authentication context
    /// </summary>
    /// <returns></returns>
    public CtfdAuthApi? GetCtfdAuth()
    {
        if (!this._flagSubmissionEnabled)
        {
            this._logger.LogInformation("Flag submission is not enabled for this challenge.");
            return null;
        }

        var context = this._httpContextAccessor.HttpContext;
        if (context == null)
        {
            this._logger.LogError("HttpContext is null.");
            return null;
        }

        var identity = context.User.Identity;
        if (identity == null || !identity.IsAuthenticated)
        {
            this._logger.LogError("User identity is null or unauthenticated.");
            return null;
        }

        //Get the right claims
        var cookie = context.User.Claims.FirstOrDefault(c => c.Type == PassThroughAuthenticationHandler.ClaimCtfdCookie)?.Value;
        var nonce = context.User.Claims.FirstOrDefault(c => c.Type == PassThroughAuthenticationHandler.ClaimCtfdNonce)?.Value;

        if (string.IsNullOrEmpty(cookie) || string.IsNullOrEmpty(nonce))
        {
            this._logger.LogError("Ctfd cookie or nonce is null or empty.");
            return null;
        }

        return new CtfdAuthApi
        {
            Cookie = cookie,
            Nonce = nonce
        };
    }

    /// <summary>
    /// Submit the flag to the CTFd server if ctfd is enabled. This method can only be called in a valid authentication context
    /// </summary>
    /// <returns></returns>
    public async Task SubmitFlagAsync(string chatId)
    {
        var auth = this.GetCtfdAuth();
        if (auth == null)
        {
            // If the auth is null, it means the flag submission is not enabled for this challenge.
            return;
        }
        await this.SubmitFlagAsync(chatId, auth);
    }

    /// <summary>
    /// Attempt to submit the flag to the CTFd server. This method does not check if the ctfd integration is enabled since it requires a valid CtfdAuthApi object.
    /// </summary>
    /// <param name="auth"></param>
    /// <returns></returns>
    public async Task SubmitFlagAsync(string chatId, CtfdAuthApi ctfdAuth)
    {
        // See if the flag is already submitted
        var challengeStatus = await this.IsAlreadySubmittedAsync(ctfdAuth, this._challengeOptions.Ctfd!.ChallengeId);
        if (challengeStatus != null && !challengeStatus.Data.SolvedByMe)
        {
            // If not, submit the flag
            var flagSubmitted = await this.PostFlagAsync(ctfdAuth, this._challengeOptions.Ctfd.ChallengeId, this._challengeOptions.Ctfd.Flag);
            if (!flagSubmitted)
            {
                this._logger.LogError("Failed to submit the flag.");
            }
            else
            {
                this._logger.LogInformation("Flag is submitted.");
                await this._messageRelayHubContext.Clients.Group(chatId).SendAsync("FlagSubmitted");
            }
        }
        else
        {
            this._logger.LogInformation("Flag is already submitted.");
        }
    }

    private async Task<CtfdResponse<CtfdChallengeResponse>?> IsAlreadySubmittedAsync(CtfdAuthApi auth, int challenge_id)
    {
        var httpClient = this._httpClient!;
        var cookie = this.FormatCookie(auth.Cookie);

        using var request = new HttpRequestMessage(HttpMethod.Get,
                                             $"/api/v1/challenges/{challenge_id}");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("CSRF-Token", auth.Nonce);

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            this._logger.LogError("Failed to get challenge info. Status code: {StatusCode}", response.StatusCode);
            return null;
        }

        //Parse the json response to an object
        var responseContent = await response.Content.ReadAsStringAsync();
        var ctfdResponse = JsonSerializer.Deserialize<CtfdResponse<CtfdChallengeResponse>>(responseContent);

        if (ctfdResponse == null)
        {
            this._logger.LogError("Failed to parse the response.");
            return null;
        }

        if (!ctfdResponse.Success)
        {
            this._logger.LogError("Failed to get the challenge info. Message: {Message}", responseContent);
            return null;
        }
        return ctfdResponse;
    }

    private async Task<bool> PostFlagAsync(CtfdAuthApi auth, int challenge_id, string flag)
    {
        var httpClient = this._httpClient!;
        var cookie = this.FormatCookie(auth.Cookie);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/challenges/attempt");
        request.Headers.Add("Cookie", cookie);
        request.Headers.Add("CSRF-Token", auth.Nonce);

        var body = new CtfdFlagSubmission
        {
            ChallengeId = challenge_id,
            Submission = flag
        };

        var json = JsonSerializer.Serialize(body);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            this._logger.LogError("Failed to submit the flag. Status code: {StatusCode}", response.StatusCode);
            return false;
        }

        //Validate the status
        var responseContent = await response.Content.ReadAsStringAsync();
        var ctfdResponse = JsonSerializer.Deserialize<CtfdResponse<CtfdFlagSubmissionResponse>>(responseContent);

        if (ctfdResponse == null)
        {
            this._logger.LogError("Failed to parse the response.");
            throw new ArgumentException("Failed to parse the response.");
        }

        if (!ctfdResponse.Success)
        {
            this._logger.LogError("Failed to submit the flag. Message: {Message}", ctfdResponse.Data.Message);
            return false;
        }

        //Validate the status
        if (ctfdResponse.Data.Status != "correct")
        {
            this._logger.LogError("Failed to submit the flag. Status: {Status}", ctfdResponse.Data.Status);
            return false;
        }

        return true;
    }

    private string FormatCookie(string cookie)
    {
        return "session=" + cookie;
    }
}
