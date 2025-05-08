// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

public class CtfdFlagSubmission
{
    [JsonPropertyName("challenge_id")]
    public int ChallengeId { get; set; } = 0;

    [JsonPropertyName("submission")]
    public string Submission { get; set; } = string.Empty;
}
