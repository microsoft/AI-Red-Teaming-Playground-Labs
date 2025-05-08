// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Models.Storage;

public class CtfdAuthApi
{
    public string Nonce { get; set; } = string.Empty;
    public string Cookie { get; set; } = string.Empty;
}
