// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Services;

public interface IMetapromptSanitizationService
{
    /// <summary>
    /// Sanitize the chat session to remove metaprompt information.
    /// </summary>
    /// <param name="chatSession"></param>
    /// <returns></returns>
    public ChatSession ChatSession(ChatSession chatSession);

    /// <summary>
    /// Sanitize the bot response prompt to remove metaprompt information.
    /// </summary>
    /// <param name="systemInstructions"></param>
    /// <param name="audience"></param>
    /// <param name="userIntent"></param>
    /// <param name="memoryText"></param>
    /// <param name="plannerDetails"></param>
    /// <param name="chatHistory"></param>
    /// <param name="promptTemplate"></param>
    /// <returns></returns>
    public BotResponsePrompt BotResponsePrompt(BotResponsePrompt prompt);

    /// <summary>
    /// Sanitize the copilot chat message to remove metaprompt information.
    /// </summary>
    /// <param name="copilotChatMessage"></param>
    /// <returns></returns>
    public CopilotChatMessage CopilotChatMessage(CopilotChatMessage copilotChatMessage);
}
