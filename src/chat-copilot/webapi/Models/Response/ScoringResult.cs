// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

public class ScoringResult
{
    [JsonPropertyName("passed")]
    public bool Passed { get; set; } = false;

    [JsonPropertyName("custom_message")]
    public string CustomMessage { get; set; } = string.Empty;
}
