// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.Json.Serialization;

namespace CopilotChat.WebApi.Models.Response;

public class ChallengeSettingsResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("metapromptLeak")]
    public bool MetapromptLeak { get; set; }

    [JsonPropertyName("fileUpload")]
    public bool Upload { get; set; }

    /// <summary>
    /// Should the plugin feature be enabled.
    /// </summary>
    [JsonPropertyName("plugins")]
    public bool Plugins { get; set; }

    /// <summary>
    /// Should the users be able to turn on or off plugins
    /// </summary>
    [JsonPropertyName("pluginsControl")]
    public bool PluginsControl { get; set; }

    /// <summary>
    /// Should the users be able to edit the plan
    /// </summary>
    [JsonPropertyName("planEdit")]
    public bool PlanEdit { get; set; }

    /// <summary>
    /// This challenge supports the human scorer
    /// </summary>
    [JsonPropertyName("humanScorer")]
    public bool HasHumanScorer { get; set; }

    /// <summary>
    /// This challenge supports the auto scorer
    /// </summary>
    [JsonPropertyName("autoScorer")]
    public bool HasAutoScorer { get; set; }

    /// <summary>
    /// Should the application be vulnerable to XSS in chat
    /// </summary>
    [JsonPropertyName("xssVulnerable")]
    public bool XssVulnerable { get; set; }

    /// <summary>
    /// Should the application show a back button in the chat.
    /// </summary>
    ///
    [JsonPropertyName("backNavigation")]
    public bool BackNavigation { get; set; }

    [JsonPropertyName("ragInput")]
    public RagSettingsResponse? RagInput { get; set; } = null;

    public class RagSettingsResponse
    {
        [JsonPropertyName("titleShort")]
        public string TitleShort { get; set; } = string.Empty;

        [JsonPropertyName("titleLong")]
        public string TitleLong { get; set; } = string.Empty;

        [JsonPropertyName("instruction1")]
        public string Instruction1 { get; set; } = string.Empty;

        [JsonPropertyName("instruction2")]
        public string Instruction2 { get; set; } = string.Empty;

        [JsonPropertyName("document")]
        public string DefaultDocument { get; set; } = string.Empty;

        [JsonPropertyName("template")]
        public string DocumentTemplate { get; set; } = string.Empty;

        [JsonPropertyName("firstMessage")]
        public string FirstMessage { get; set; } = string.Empty;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("isReadOnly")]
        public bool IsReadOnly { get; set; }

        [JsonPropertyName("maxTurns")]
        public int MaxNumberOfTurns { get; set; } = 0;
    }
}
