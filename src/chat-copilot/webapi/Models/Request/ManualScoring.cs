// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

public class ManualScoring
{
    [JsonPropertyName("chatId")]
    public string ChatId { get; set; } = string.Empty;

    [JsonPropertyName("messageIndex")]
    public int MessageIndex { get; set; } = 0;
}
