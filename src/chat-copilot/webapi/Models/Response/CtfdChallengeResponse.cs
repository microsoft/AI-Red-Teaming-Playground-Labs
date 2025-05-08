// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

public class CtfdChallengeResponse
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; set; } = 0;

    [JsonPropertyName("value")]
    public int Value { get; set; } = 0;

    [JsonPropertyName("solved_by_me")]
    public bool SolvedByMe { get; set; } = false;
}
