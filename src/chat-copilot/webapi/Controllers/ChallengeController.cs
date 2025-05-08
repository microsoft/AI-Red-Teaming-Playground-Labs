// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Services.Ctfd;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CopilotChat.WebApi.Controllers;
[ApiController]
public class ChallengeController : ControllerBase
{
    private readonly ChallengeOptions _challengeOptions;
    private readonly PromptsOptions _promptsOptions;
    private readonly IDictionary<string, Plugin> _availablePlugins;
    private readonly HttpClient _httpClientScorer;
    private readonly ChatSessionRepository _sessionRepository;
    private readonly ChatMessageRepository _messageRepository;
    private readonly IMetapromptSanitizationService _metapromptSanitize;
    private readonly ILogger<ChallengeController> _logger;
    private readonly IPrometheusTelemetryService _prometheusTelemetryService;
    private readonly ICtfdScorerMessageService _ctfdScorerMessageService;
    private readonly ICtfdFlagSubmissionService _ctfdFlagSubmissionService;
    private readonly IRagService _ragService;

    public ChallengeController(
        IOptions<ChallengeOptions> challengeOptions,
        IOptions<PromptsOptions> promptsOptions,
        IDictionary<string, Plugin> availablePlugins,
        IHttpClientFactory httpClientFactory,
        ChatSessionRepository chatSessionRepository,
        ChatMessageRepository chatMessageRepository,
        IMetapromptSanitizationService metapromptSanitizationService,
        ILogger<ChallengeController> logger,
        IPrometheusTelemetryService prometheusTelemetryService,
        ICtfdScorerMessageService ctfdScorerMessageService,
        ICtfdFlagSubmissionService ctfdFlagSubmissionService,
        IRagService ragService)
    {
        this._challengeOptions = challengeOptions.Value;
        this._promptsOptions = promptsOptions.Value;

        this._availablePlugins = availablePlugins;
        this._sessionRepository = chatSessionRepository;
        this._messageRepository = chatMessageRepository;
        this._metapromptSanitize = metapromptSanitizationService;
        this._logger = logger;
        this._prometheusTelemetryService = prometheusTelemetryService;
        this._ctfdScorerMessageService = ctfdScorerMessageService;
        this._ctfdFlagSubmissionService = ctfdFlagSubmissionService;
        this._ragService = ragService;
        this._httpClientScorer = httpClientFactory.CreateClient();

        if (this._challengeOptions.HumanScorer != null)
        {
            this._httpClientScorer.BaseAddress = new Uri(this._challengeOptions.HumanScorer.Endpoint);
            this._httpClientScorer.DefaultRequestHeaders.Add("x-scoring-key", this._challengeOptions.HumanScorer.ApiKey);
        }
    }

    [HttpGet]
    [Route("challenge/settings")]
    public Task<ActionResult<ChallengeSettingsResponse>> GetSettings()
    {
        return Task.FromResult<ActionResult<ChallengeSettingsResponse>>(
            new ChallengeSettingsResponse
            {
                Id = this._challengeOptions.Id,
                Name = this._challengeOptions.Name,
                Description = this._challengeOptions.Description,
                MetapromptLeak = this._challengeOptions.MetapromptLeak,
                Upload = this._challengeOptions.Upload,
                Plugins = this._availablePlugins.Count > 0,
                PluginsControl = this._challengeOptions.PluginsControl,
                HasHumanScorer = this._challengeOptions.HumanScorer != null,
                HasAutoScorer = this._challengeOptions.Scorer != null,
                PlanEdit = this._challengeOptions.PlanEdit,
                XssVulnerable = this._challengeOptions.XssVulnerable,
                BackNavigation = this._challengeOptions.BackNavigation,

                RagInput = this._challengeOptions.RagInput == null ?
                new ChallengeSettingsResponse.RagSettingsResponse
                {
                    IsReadOnly = false,
                    Enabled = false,
                } : new ChallengeSettingsResponse.RagSettingsResponse
                {
                    TitleShort = this._challengeOptions.RagInput.TitleShort,
                    TitleLong = this._challengeOptions.RagInput.TitleLong,
                    Instruction1 = this._challengeOptions.RagInput.Instruction1,
                    Instruction2 = this._challengeOptions.RagInput.Instruction2,
                    DefaultDocument = this._challengeOptions.RagInput.DefaultDocument,
                    DocumentTemplate = this._challengeOptions.RagInput.DocumentTemplate,
                    FirstMessage = this._challengeOptions.RagInput.FirstMessage,
                    IsReadOnly = this._challengeOptions.RagInput.IsReadOnly,
                    MaxNumberOfTurns = this._challengeOptions.RagInput.LockAfter,
                    Enabled = true
                }
            }
        );
    }

    /// <summary>
    /// Endpoint used by the manual scorer to send the scoring result
    /// </summary>
    /// <param name="messageRelayHubContext"></param>
    /// <param name="scoringResult"></param>
    /// <param name="chatId"></param>
    /// <returns></returns>
    [HttpPost]
    [AllowAnonymous]
    [Route("chats/{chatId:guid}/scoring/receive")]
    public async Task<ActionResult> ReceiveManualScoring(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] ScoringResult scoringResult,
        Guid chatId)
    {
        if (this._challengeOptions.HumanScorer == null)
        {
            return this.Unauthorized("Human scorer is not enabled.");
        }

        if (this.Request.Headers["x-scoring-key"] != this._challengeOptions.HumanScorer!.ApiKey)
        {
            this._logger.LogWarning("The request does not have the correct scoring key");
            return this.Unauthorized();
        }

        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat == null)
            {
                this._logger.LogWarning("The chat {ChatId} could not be found can't receive result.", chatId);
                return this.NoContent();
            }

            if (chat.IsDeleted)
            {
                this._logger.LogWarning("The chat {ChatId} was deleted can't receive result.", chatId);
                return this.NoContent();
            }

            if (!chat.IsLocked)
            {
                this._logger.LogWarning("The chat {ChatId} is not locked can't receive result.", chatId);
                return this.NoContent();
            }

            var scorerContent = "**Manual scorer message**: ";

            if (scoringResult.Passed)
            {
                if (chat.CtfdAuth != null)
                {
                    // Submit the flag if we have a ctfd auth object.
                    await this._ctfdFlagSubmissionService.SubmitFlagAsync(chatId.ToString(), chat.CtfdAuth);
                }
                scorerContent += "You have passed the challenge. Congratulations!";
                scorerContent += this._ctfdScorerMessageService.GetScorerMessage();
            }
            else
            {
                scorerContent += "The challenge is not solved according to the reviewer.";
            }

            if (!string.IsNullOrEmpty(scoringResult.CustomMessage))
            {
                scorerContent += "\n\n" + "**Message from the reviewer**: " + scoringResult.CustomMessage;
            }

            //Send the new message to the chat
            var scorerMessage = CopilotChatMessage.CreateBotResponseMessage(chatId.ToString(), scorerContent, string.Empty, null);
            await this._messageRepository.UpsertAsync(scorerMessage);
            await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", chatId, "", this._metapromptSanitize.CopilotChatMessage(scorerMessage));

            //Unlock the chat
            chat.IsLocked = false;
            await this._sessionRepository.UpsertAsync(chat);
            await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync(ChatHistoryController.ChatEditedClientCall, this._metapromptSanitize.ChatSession(chat));

            if (scoringResult.Passed)
            {
                this._prometheusTelemetryService.RecordMetric(MetricName.SuccessManualScorerCounter, 1);
            }
            else
            {
                this._prometheusTelemetryService.RecordMetric(MetricName.FailureManualScorerCounter, 1);
            }

            return this.NoContent();
        }

        this._logger.LogWarning("The chat {ChatId} could not be found can't receive result.", chatId);
        return this.NoContent();
    }

    [HttpPost]
    [Route("chats/{chatId:guid}/scoring/manual")]
    [Authorize(AuthzChallenge.HumanScorer)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult> ManualScoring(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] ManualScoring manualScoring,
        Guid chatId
        )
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat == null)
            {
                return this.NotFound("The chat does not exists");
            }
            if (chat.IsDeleted)
            {
                return this.NotFound("Chat session is deleted");
            }
            else if (chat.IsLocked)
            {
                return this.BadRequest("Chat session is locked");
            }

            //Check if the message index is valid
            if (manualScoring.MessageIndex <= 0)
            {
                return this.BadRequest("Can't grade the first message");
            }

            var messages = await this._messageRepository.FindByChatIdAsync(chatId.ToString());
            var sortedMessages = messages.OrderBy(m => m.Timestamp);

            //We make sure that it's a bot message
            var message = sortedMessages.ElementAtOrDefault(manualScoring.MessageIndex);
            if (message == null)
            {
                return this.NotFound("The message does not exists. Please validate the MessageIndex parameter");
            }

            if (message.AuthorRole != CopilotChatMessage.AuthorRoles.Bot)
            {
                return this.BadRequest("Can only grade bot messages");
            }

            if (message.Type != CopilotChatMessage.ChatMessageType.Message)
            {
                return this.BadRequest("Can only grade text messages");
            }

            //Make an http request to the manual scorer endpoint
            var answerUri = new UriBuilder(this._challengeOptions.HumanScorer!.ChatCopilotEndpoint);
            answerUri.Path = $"/chats/{chatId}/scoring/receive";

            var request = new ScoringIntake
            {
                ChallengeId = this._challengeOptions.Id,
                Title = this._challengeOptions.Name,
                Goal = this._promptsOptions.InitialBotMessage,
                Conversation = sortedMessages.Select(m => new ScoringIntake.ScoringIntakeMessage
                {
                    Message = m.Content,
                    Role = (int)m.AuthorRole
                }),
                Timestamp = DateTime.UtcNow,
                ChatId = chatId,
                AnswerUri = answerUri.ToString(),
                Document = this._ragService.GetDocument(chat.RagDocument, chat.RagUserInput)
            };

            var response = await this._httpClientScorer.PostAsJsonAsync("/api/score", request);
            response.EnsureSuccessStatusCode();

            //Lock the chat
            chat.IsLocked = true;

            //Set the ctfd auth to the chat
            chat.CtfdAuth = this._ctfdFlagSubmissionService.GetCtfdAuth();

            await this._sessionRepository.UpsertAsync(chat);
            await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync(ChatHistoryController.ChatEditedClientCall, this._metapromptSanitize.ChatSession(chat));

            this._prometheusTelemetryService.RecordMetric(MetricName.ManualScorerCounter, 1);

            return this.Accepted();
        }

        return this.NotFound($"The chat {chatId} could not be found");
    }

    [HttpPost]
    [Route("chats/{chatId:guid}/scoring/xss")]
    [Authorize(AuthzChallenge.XssVulnerable)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]
    public async Task<ActionResult> XssVulnerable(
               [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
               Guid chatId)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat == null)
            {
                return this.NotFound("The chat does not exists");
            }
            if (chat.IsDeleted)
            {
                return this.NotFound("Chat session is deleted");
            }
            else if (chat.IsLocked)
            {
                return this.BadRequest("Chat session is locked");
            }

            if (!chat.XssTriggered)
            {
                await this._ctfdFlagSubmissionService.SubmitFlagAsync(chatId.ToString());

                var scorerContent = "**XSS**: Congratulations you ran an XSS successfully by abusing an LLM.";
                scorerContent += this._ctfdScorerMessageService.GetScorerMessage();

                var scorerMessage = CopilotChatMessage.CreateBotResponseMessage(chatId.ToString(), scorerContent, string.Empty, null);
                await this._messageRepository.UpsertAsync(scorerMessage);
                await messageRelayHubContext.Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", chatId, "", this._metapromptSanitize.CopilotChatMessage(scorerMessage));

                chat.XssTriggered = true;
                await this._sessionRepository.UpsertAsync(chat);

                this._prometheusTelemetryService.RecordMetric(MetricName.SuccessAutoScorerCounter, 1);
            }
            return this.NoContent();
        }
        return this.NotFound($"The chat {chatId} could not be found");
    }

    [HttpPost]
    [Route("chats/{chatId:guid}/rag")]
    [Authorize(AuthzChallenge.RagInput)]
    [Authorize(Policy = AuthPolicyName.RequireChatParticipant)]

    public async Task<ActionResult> RagInput(
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] RagInput ragInput,
        Guid chatId)
    {
        ChatSession? chat = null;
        if (await this._sessionRepository.TryFindByIdAsync(chatId.ToString(), callback: v => chat = v))
        {
            if (chat == null)
            {
                return this.NotFound("The chat does not exists");
            }
            if (chat.IsDeleted)
            {
                return this.NotFound("Chat session is deleted");
            }

            //Check if the chat has more than one message.
            var messages = await this._messageRepository.FindByChatIdAsync(chatId.ToString());
            if (messages.Count() > 1)
            {
                return this.BadRequest("The chat can't set a new RAG after the first message.");
            }

            if (this._challengeOptions.RagInput!.IsReadOnly)
            {
                if (!string.IsNullOrEmpty(ragInput.Document))
                {
                    return this.BadRequest("Document can't be set for read only");
                }

                chat.RagDocument = this._challengeOptions.RagInput.DefaultDocument;
                chat.RagUserInput = ragInput.UserInput!;
            }
            else
            {
                if (!string.IsNullOrEmpty(ragInput.UserInput))
                {
                    return this.BadRequest("UserInput can't be set for non readonly.");
                }
                chat.RagDocument = ragInput.Document!;
            }

            await this._sessionRepository.UpsertAsync(chat);
            return this.NoContent();
        }
        return this.NotFound($"The chat {chatId} could not be found");
    }
}
