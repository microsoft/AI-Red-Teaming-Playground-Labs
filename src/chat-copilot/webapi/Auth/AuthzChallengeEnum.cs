// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Auth;

public static class AuthzChallenge
{
    /// <summary>
    /// Allows a user to upload files to the chat
    /// </summary>
    public const string Upload = "upload";

    /// <summary>
    /// Allows a user to enable/disable plugins in chats
    /// </summary>
    public const string PluginsControl = "pluginsControl";

    /// <summary>
    /// Allows a user to interact with plugins. This flag is controlled by the plugins array
    /// </summary>
    public const string Plugins = "plugins";

    /// <summary>
    /// Allows a user to use the human scorer
    /// </summary>
    public const string HumanScorer = "humanScorer";

    /// <summary>
    /// Allows the user to make a request to the XSS vulnreable endpoint to see the scoring message
    /// </summary>
    public const string XssVulnerable = "xssVulnerable";

    public const string RagInput = "ragInput";
}
