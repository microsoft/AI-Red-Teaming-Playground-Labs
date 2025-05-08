// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Auth;

public class CtfdAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();
    private readonly IOptions<ChallengeOptions> _challengeOptions;

    public CtfdAuthorizationMiddlewareResultHandler(
        IOptions<ChallengeOptions> challengeOptions)
    {
        this._challengeOptions = challengeOptions;
    }

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        if (this._challengeOptions.Value.Ctfd != null)
        {
            //Ctfd is enabled
            if (!authorizeResult.Succeeded)
            {
                var reason = "Undefined reason";

                if (context.Items.ContainsKey(PassThroughAuthenticationHandler.ContextReasonName))
                {
                    var contextReason = (string?)context.Items[PassThroughAuthenticationHandler.ContextReasonName];
                    reason = contextReason ?? reason;
                }

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(new AuthErrorResponse
                {
                    AuthType = "ctfd",
                    Error = reason,
                    RedirectUri = this._challengeOptions.Value.Ctfd.RedirectUrl?.ToString() ?? "",
                });

                await context.Response.WriteAsync(json);
                return;
            }
        }
        await this._defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }
}
