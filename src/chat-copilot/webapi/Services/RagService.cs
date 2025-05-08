// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Services;

public class RagService : IRagService
{
    private readonly ChallengeOptions.RagInputOptions? _ragOptions;

    public RagService(IOptions<ChallengeOptions> options)
    {
        this._ragOptions = options.Value.RagInput;
    }

    public bool IsRagEnabled()
    {
        return this._ragOptions != null;
    }

    public string GetDocument(string? ragDocument, string? ragUserInput)
    {
        if (this._ragOptions != null)
        {
            if (this._ragOptions.IsReadOnly)
            {
                var document = this._ragOptions.DefaultDocument;
                if (string.IsNullOrEmpty(this._ragOptions.DocumentTemplate))
                {
                    document += "\n" + (ragUserInput ?? "");
                }
                else
                {
                    document = document.Replace(this._ragOptions.DocumentTemplate, ragUserInput ?? "", StringComparison.InvariantCultureIgnoreCase);
                }
                return document;
            }

            return ragDocument ?? this._ragOptions.DefaultDocument;
        }
        return "";
    }
}
