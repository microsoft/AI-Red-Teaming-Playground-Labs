// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Services.Ctfd;

public class CtfdScorerMessageService : ICtfdScorerMessageService
{
    private readonly bool _flagSubmissionEnabled;
    private readonly string _flagValue;

    public CtfdScorerMessageService(IOptions<ChallengeOptions> challengeOptions)
    {
        this._flagSubmissionEnabled = false;
        this._flagValue = "";

        if (challengeOptions.Value.Ctfd != null)
        {
            this._flagSubmissionEnabled = true;
            this._flagValue = challengeOptions.Value.Ctfd.Flag;
        }
    }

    public string GetScorerMessage()
    {
        if (!this._flagSubmissionEnabled)
        {
            return string.Empty;
        }

        return $"\n\nHere's the flag for this challlenge that was already submitted on your behalf: \"{this._flagValue}\".";
    }
}
