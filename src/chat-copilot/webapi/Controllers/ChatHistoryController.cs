﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Plugins.Utils;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller for chat history.
/// This controller is responsible for creating new chat sessions, retrieving chat sessions,
/// retrieving chat messages, and editing chat sessions.
/// </summary>
[ApiController]
public class ChatHistoryController : ControllerBase
{
    public const string ChatEditedClientCall = "ChatEdited";
    private const string ChatDeletedClientCall = "ChatDeleted";
    private const string GetChatRoute = "GetChatRoute";

    private readonly ILogger<ChatHistoryController> _logger;
    private readonly IKernelMemory _memoryClient;
    private readonly ChatSessionRepository _sessionRepository;
    private readonly ChatMessageRepository _messageRepository;
    private readonly ChatParticipantRepository _participantRepository;
    private readonly ChatMemorySourceRepository _sourceRepository;
    private readonly IMetapromptSanitizationService _metapromptSanitize;
    private readonly IDictionary<string, Plugin> _availablePlugins;
    private readonly ChallengeOptions _challengeOptions;
    private readonly PromptsOptions _promptOptions;
    private readonly IAuthInfo _authInfo;
    private readonly IPrometheusTelemetryService _prometheusTelemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatHistoryController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="memoryClient">Memory client.</param>
    /// <param name="sessionRepository">The chat session repository.</param>
    /// <param name="messageRepository">The chat message repository.</param>
    /// <param name="participantRepository">The chat participant repository.</param>
    /// <param name="sourceRepository">The chat memory resource repository.</param>
    /// <param name="promptsOptions">The prompts options.</param>
    /// <param name="authInfo">The auth info for the current request.</param>
    public ChatHistoryController(
        ILogger<ChatHistoryController> logger,
        IKernelMemory memoryClient,
        ChatSessionRepository sessionRepository,
        ChatMessageRepository messageRepository,
        ChatParticipantRepository participantRepository,
        ChatMemorySourceRepository sourceRepository,
        IOptions<PromptsOptions> promptsOptions,
        IMetapromptSanitizationService metapromptSanitize,
        IDictionary<string, Plugin> availablePlugins,
        IOptions<ChallengeOptions> challengeOptions,
        IAuthInfo authInfo,
        IPrometheusTelemetryService prometheusTelemetryService)
    {
        this._logger = logger;
        this._memoryClient = memoryClient;
        this._sessionRepository = sessionRepository;
        this._messageRepository = messageRepository;
        this._participantRepository = participantRepository;
        this._sourceRepository = sourceRepository;
        this._metapromptSanitize = metapromptSanitize;
        this._availablePlugins = availablePlugins;
        this._challengeOptions = challengeOptions.Value;
        this._promptOptions = promptsOptions.Value;
        this._authInfo = authInfo;
        this._prometheusTelemetryService = prometheusTelemetryService;
    }

    /// <summary>
    /// Create a new chat session and populate the session with the initial bot message.
    /// </summary>
    /// <param name="chatParameter">Contains the title of the chat.</param>
    /// <returns>The HTTP action result.</returns>
    [HttpPost]
    [Route("chats")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChatSessionAsync(
        [FromBody] CreateChatParameters chatParameters)
    {
        if (chatParameters.Title == null)
        {
            return this.BadRequest("Chat session parameters cannot be null.");
        }

        // Create a new chat session
        var newChat = new ChatSession(chatParameters.Title, this._promptOptions.SystemDescription);
        if (!this._challengeOptions.PluginsControl && this._availablePlugins.Count > 0)
        {
            //We add the plugins by default to the chat. It should be enabled
            foreach (var pluginName in this._availablePlugins.Keys)
            {
                newChat.EnabledPlugins.Add(pluginName);
            }
        }
        await this._sessionRepository.CreateAsync(newChat);

        // Create initial bot message
        var chatMessage = CopilotChatMessage.CreateBotResponseMessage(
            newChat.Id,
            this._promptOptions.InitialBotMessage,
            string.Empty, // The initial bot message doesn't need a prompt.
            null,
            TokenUtils.EmptyTokenUsages());
        await this._messageRepository.CreateAsync(chatMessage);

        // Add the user to the chat session
        await this._participantRepository.CreateAsync(new ChatParticipant(this._authInfo.UserId, newChat.Id));

        this._logger.LogDebug("Created chat session with id {0}.", newChat.Id);
        this._prometheusTelemetryService.RecordMetric(MetricName.ChatSessionCounter, 1);

        return this.CreatedAtRoute(GetChatRoute, new { chatId = newChat.Id }, new CreateChatResponse(this._metapromptSanitize.ChatSession(newChat), chatMessage));
    }

    /// <summary>
    /// Get a chat session by id.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    [HttpGet]
    [Route("chats/{chatId:guid}", Name = GetChatRoute)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatSessionByIdAsync(Guid chatId)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat != null && chat.IsDeleted)
            {
                return this.NotFound($"Chat session with id '{chatId}' is deleted.");
            }
            return this.Ok(this._metapromptSanitize.ChatSession(chat!));
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Get all chat sessions associated with the logged in user. Return an empty list if no chats are found.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A list of chat sessions. An empty list if the user is not in any chat session.</returns>
    [HttpGet]
    [Route("chats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllChatSessionsAsync()
    {
        // Get all participants that belong to the user.
        // Then get all the chats from the list of participants.
        var chatParticipants = await this._participantRepository.FindByUserIdAsync(this._authInfo.UserId);

        var chats = new List<ChatSession>();
        foreach (var chatParticipant in chatParticipants)
        {
            ChatSession? chat = null;
            if (await this._sessionRepository.TryFindByIdAsync(chatParticipant.ChatId, callback: v => chat = v))
            {
                if (chat != null && chat.IsDeleted)
                {
                    continue;
                }

                chats.Add(this._metapromptSanitize.ChatSession(chat!));
            }
            else
            {
                this._logger.LogDebug("Failed to find chat session with id {0}", chatParticipant.ChatId);
            }
        }

        return this.Ok(chats);
    }

    /// <summary>
    /// Get all chat messages for a chat session.
    /// The list will be ordered with the first entry being the most recent message.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <param name="startIdx">The start index at which the first message will be returned.</param>
    /// <param name="count">The number of messages to return. -1 will return all messages starting from startIdx.</param>
    [HttpGet]
    [Route("chats/{chatId:guid}/messages")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> GetChatMessagesAsync(
        [FromRoute] Guid chatId,
        [FromQuery] int startIdx = 0,
        [FromQuery] int count = -1)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat != null && chat.IsDeleted)
            {
                return this.NotFound($"No messages found for chat id '{chatId}'.");
            }
        }

        // TODO:  [Issue #48] the code mixes strings and Guid without being explicit about the serialization format
        var chatMessages = await this._messageRepository.FindByChatIdAsync(chatId.ToString());
        if (!chatMessages.Any())
        {
            return this.NotFound($"No messages found for chat id '{chatId}'.");
        }

        chatMessages = chatMessages.OrderByDescending(m => m.Timestamp).Skip(startIdx);
        if (count >= 0) { chatMessages = chatMessages.Take(count); }

        chatMessages = chatMessages.Select(c => this._metapromptSanitize.CopilotChatMessage(c));

        return this.Ok(chatMessages);
    }

    /// <summary>
    /// Edit a chat session.
    /// </summary>
    /// <param name="chatParameters">Object that contains the parameters to edit the chat.</param>
    [HttpPatch]
    [Route("chats/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> EditChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] EditChatParameters chatParameters,
        [FromRoute] Guid chatId)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat != null && chat.IsDeleted)
            {
                return this.NotFound($"No chat session found for chat id '{chatId}'.");
            }

            chat!.Title = chatParameters.Title ?? chat!.Title;
            chat!.SystemDescription = chatParameters.SystemDescription ?? chat!.SafeSystemDescription;
            chat!.MemoryBalance = chatParameters.MemoryBalance ?? chat!.MemoryBalance;
            await this._sessionRepository.UpsertAsync(chat);
            await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync(ChatEditedClientCall, this._metapromptSanitize.ChatSession(chat));

            return this.Ok(this._metapromptSanitize.ChatSession(chat));
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Gets list of imported documents for a given chat.
    /// </summary>
    [Route("chats/{chatId:guid}/documents")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    [Authorize(AuthzChallenge.Upload)]
    public async Task<ActionResult<IEnumerable<MemorySource>>> GetSourcesAsync(Guid chatId)
    {
        this._logger.LogInformation("Get imported sources of chat session {0}", chatId);

        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat != null && chat.IsDeleted)
            {
                return this.NotFound($"No chat session found for chat id '{chatId}'.");
            }

            IEnumerable<MemorySource> sources = await this._sourceRepository.FindByChatIdAsync(chatId.ToString());

            return this.Ok(sources);
        }

        return this.NotFound($"No chat session found for chat id '{chatId}'.");
    }

    /// <summary>
    /// Delete a chat session.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    [HttpDelete]
    [Route("chats/{chatId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<IActionResult> DeleteChatSessionAsync(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        Guid chatId,
        CancellationToken cancellationToken)
    {
        var chatIdString = chatId.ToString();
        ChatSession? chatToDelete = null;
        try
        {
            // Make sure the chat session exists
            chatToDelete = await this._sessionRepository.FindByIdAsync(chatIdString);
            if (chatToDelete.IsDeleted)
            {
                return this.NotFound($"No chat session found for chat id '{chatId}'.");
            }
        }
        catch (KeyNotFoundException)
        {
            return this.NotFound($"No chat session found for chat id '{chatId}'.");
        }

        // Delete any resources associated with the chat session.
        try
        {
            await this.DeleteChatResourcesAsync(chatIdString, cancellationToken);
        }
        catch (AggregateException)
        {
            return this.StatusCode(500, $"Failed to delete resources for chat id '{chatId}'.");
        }

        // Delete chat session and broadcast update to all participants.
        chatToDelete.IsDeleted = true;
        await this._sessionRepository.UpsertAsync(chatToDelete);
        await messageRelayHubContext.Clients.Group(chatIdString).SendAsync(ChatDeletedClientCall, chatIdString, this._authInfo.UserId, cancellationToken: cancellationToken);

        this._prometheusTelemetryService.RecordMetric(MetricName.ChatSessionDeleteCounter, 1);

        return this.NoContent();
    }

    /// <summary>
    /// Deletes all associated resources (messages, memories, participants) associated with a chat session.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    private async Task DeleteChatResourcesAsync(string chatId, CancellationToken cancellationToken)
    {
        var cleanupTasks = new List<Task>();

        // Create and store the tasks for deleting all users tied to the chat.
        var participants = await this._participantRepository.FindByChatIdAsync(chatId);
        foreach (var participant in participants)
        {
            cleanupTasks.Add(this._participantRepository.DeleteAsync(participant));
        }

        //Disabled the deletion of messages
        /*
        // Create and store the tasks for deleting chat messages.
        var messages = await this._messageRepository.FindByChatIdAsync(chatId);
        foreach (var message in messages)
        {
            cleanupTasks.Add(this._messageRepository.DeleteAsync(message));
        }
        */

        // Create and store the tasks for deleting memory sources.
        var sources = await this._sourceRepository.FindByChatIdAsync(chatId, false);
        foreach (var source in sources)
        {
            cleanupTasks.Add(this._sourceRepository.DeleteAsync(source));
        }

        // Create and store the tasks for deleting semantic memories.
        cleanupTasks.Add(this._memoryClient.RemoveChatMemoriesAsync(this._promptOptions.MemoryIndexName, chatId, cancellationToken));

        // Create a task that represents the completion of all cleanupTasks
        Task aggregationTask = Task.WhenAll(cleanupTasks);
        try
        {
            // Await the completion of all tasks in parallel
            await aggregationTask;
        }
        catch (Exception ex)
        {
            // Handle any exceptions that occurred during the tasks
            if (aggregationTask?.Exception?.InnerExceptions != null && aggregationTask.Exception.InnerExceptions.Count != 0)
            {
                foreach (var innerEx in aggregationTask.Exception.InnerExceptions)
                {
                    this._logger.LogInformation("Failed to delete an entity of chat {0}: {1}", chatId, innerEx.Message);
                }

                throw aggregationTask.Exception;
            }

            throw new AggregateException($"Resource deletion failed for chat {chatId}.", ex);
        }
    }
}
