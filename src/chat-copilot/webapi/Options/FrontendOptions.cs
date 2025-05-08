// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Options;

/// <summary>
/// Configuration options to be relayed to the frontend.
/// </summary>
public sealed class FrontendOptions
{
    public const string PropertyName = "Frontend";

    /// <summary>
    /// Client ID for the frontend
    /// </summary>
    public string AadClientId { get; set; } = string.Empty;
}
