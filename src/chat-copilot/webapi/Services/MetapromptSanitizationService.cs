// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using ChatCompletionContextMessages = Microsoft.SemanticKernel.AI.ChatCompletion.ChatHistory;

namespace CopilotChat.WebApi.Services;

public class MetapromptSanitizationService : IMetapromptSanitizationService
{
    private ChallengeOptions _challengeOptions;

    public MetapromptSanitizationService(IOptions<ChallengeOptions> challengeOptions)
    {
        this._challengeOptions = challengeOptions.Value;
    }

    public ChatSession ChatSession(ChatSession chatSession)
    {
        var metapromtLeak = this._challengeOptions.MetapromptLeak;
        if (!metapromtLeak)
        {
            chatSession.SystemDescription = "";
        }

        //Always remove the CtfdAuth object from the chat session so it is not leaked on the client side
        chatSession.CtfdAuth = null;
        return chatSession;
    }

    public CopilotChatMessage CopilotChatMessage(CopilotChatMessage copilotChatMessage)
    {
        var metapromtLeak = this._challengeOptions.MetapromptLeak;
        if (!metapromtLeak)
        {
            copilotChatMessage.Prompt = "";
        }
        return copilotChatMessage;
    }

    public BotResponsePrompt BotResponsePrompt(BotResponsePrompt prompt)
    {
        var metapromptLeak = this._challengeOptions.MetapromptLeak;

        ChatCompletionContextMessages cleanPromptTemplate;
        if (!metapromptLeak)
        {
            cleanPromptTemplate = new ChatCompletionContextMessages();
            foreach (var message in prompt.MetaPromptTemplate)
            {
                if (message.Role == AuthorRole.User || message.Role == AuthorRole.Assistant)
                {
                    //We intentionaly leave out system messages
                    cleanPromptTemplate.Add(message);
                }
            }
            return new BotResponsePrompt(
                "",
                prompt.Audience,
                "",
                prompt.PastMemories,
                prompt.ExternalInformation, //metapromptLeak ? plannerDetails : new SemanticDependency<PlanExecutionMetadata>(""),
                prompt.ChatHistory,
                cleanPromptTemplate
            );
        }

        return prompt;
    }
}
