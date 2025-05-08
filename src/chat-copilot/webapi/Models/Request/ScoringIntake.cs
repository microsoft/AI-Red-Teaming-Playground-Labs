// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

public class ScoringIntake
{
    [JsonPropertyName("challenge_id")]
    public int ChallengeId { get; set; } = 0;

    [JsonPropertyName("challenge_goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("challenge_title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("conversation")]
    public IEnumerable<ScoringIntakeMessage>? Conversation { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("conversation_id")]
    public Guid ChatId { get; set; } = Guid.Empty;

    [JsonPropertyName("document")]
    public string Document { get; set; } = string.Empty;

    [JsonPropertyName("answer_uri")]
    public string AnswerUri { get; set; } = string.Empty;

    public class ScoringIntakeMessage
    {
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public int Role { get; set; } = 0;
    }
}
