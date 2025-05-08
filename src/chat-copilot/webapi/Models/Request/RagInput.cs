// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Request;

public class RagInput
{
    [JsonPropertyName("document")]
    public string? Document { get; set; }

    [JsonPropertyName("userInput")]
    public string? UserInput { get; set; }
}
