// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Services;

public interface IRagService
{
    bool IsRagEnabled();
    string GetDocument(string? ragDocument, string? ragUserInput);
}
