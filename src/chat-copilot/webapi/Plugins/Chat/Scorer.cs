// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Services.Ctfd;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;
using Newtonsoft.Json;

namespace CopilotChat.WebApi.Plugins.Chat;

public class Scorer
{
    public const string ScorerVariableName = "ScorerResult";
    public const string ScorerReasonVariableName = "ScorerReason";

    private const string ScorerKeywordTrue = "[true]";
    private const string ScorerKeywordFalse = "[false]";

    private const string ContentPlaceholder = "{{Content}}";

    private readonly ILogger<Scorer> _logger;
    private readonly IPrometheusTelemetryService _prometheusTelemetryService;
    private readonly ICtfdFlagSubmissionService _ctfdFlagSubmissionService;
    private readonly ICtfdScorerMessageService _ctfdScorerMessageService;
    private ChallengeOptions _challengeOptions;

    public Scorer(IOptions<ChallengeOptions> challengeOptions,
        ILogger<Scorer> logger,
        IPrometheusTelemetryService prometheusTelemetryService,
        ICtfdFlagSubmissionService ctfdFlagSubmissionService,
        ICtfdScorerMessageService ctfdScorerMessageService)
    {
        this._challengeOptions = challengeOptions.Value;
        this._logger = logger;
        this._prometheusTelemetryService = prometheusTelemetryService;
        this._ctfdFlagSubmissionService = ctfdFlagSubmissionService;
        this._ctfdScorerMessageService = ctfdScorerMessageService;

        //Validate if the metaprompt format
        if (this.IsEnabled && !this._challengeOptions.Scorer!.IsRegex)
        {
            if (!this._challengeOptions.Scorer.Metaprompt.Contains(ContentPlaceholder, StringComparison.OrdinalIgnoreCase))
            {
                this._logger.LogError("Scorer metaprompt does not contain the content placeholder: {ContentPlaceholder}", ContentPlaceholder);
            }
        }
    }
    public bool IsEnabled => this._challengeOptions.Scorer != null;

    public bool HasReason => this._challengeOptions.Scorer != null && !this._challengeOptions.Scorer.IsRegex;

    public string SuccessMessage => "**Scorer message**: " + (this._challengeOptions.Scorer?.SuccessMessage ?? "") + this._ctfdScorerMessageService.GetScorerMessage();

    public async Task<SKContext> ScoreBotMessageAsync(
        string chatId,
        string message,
        SKContext context,
        CancellationToken ct = default)
    {
        this._logger.LogInformation("Scorer called. Evaluating message: {Message}", message);
        var scorerResult = false;
        //If there is a scorer engine
        if (this._challengeOptions.Scorer != null)
        {
            var scorerOptions = this._challengeOptions.Scorer;

            if (scorerOptions.IsRegex)
            {
                var options = scorerOptions.IsCaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                if (Regex.IsMatch(message, scorerOptions.Instruction, options))
                {
                    scorerResult = true;
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(scorerOptions.Instruction))
                {
                    var metaprompt = this.GetMetaprompt(scorerOptions.Instruction, message);
                    this._logger.LogInformation("Scorer metaprompt: {Metaprompt}", metaprompt);
                    var responses = await context.ServiceProvider.GetTextCompletionServiceOrDefault().GetCompletionsAsync(metaprompt, null, ct);
                    foreach (var response in responses)
                    {
                        var modelResult = response.ModelResult!;
                        var chatResult = modelResult.GetOpenAIChatResult();
                        this._prometheusTelemetryService.RecordMetric(MetricName.TokenCounter, chatResult.Usage.TotalTokens);

                        var answer = chatResult.Choice.Message.Content;
                        this._logger.LogInformation("Scorer completion: {Completion}", answer);
                        var reason = "";
                        if (answer.Contains(ScorerKeywordTrue, StringComparison.OrdinalIgnoreCase))
                        {
                            this._logger.LogInformation("Scorer completion contains [true]. Returning true.");
                            reason = answer.Replace(ScorerKeywordTrue, "", StringComparison.OrdinalIgnoreCase).Trim();
                            scorerResult = true;
                        }
                        else if (answer.Contains(ScorerKeywordFalse, StringComparison.OrdinalIgnoreCase))
                        {
                            this._logger.LogInformation("Scorer completion contains [false]. Returning false.");
                            reason = answer.Replace(ScorerKeywordFalse, "", StringComparison.OrdinalIgnoreCase).Trim();
                        }
                        else
                        {
                            this._logger.LogWarning("Scorer completion does not contain [true] or [false]. Returning false.");
                        }

                        context.Variables.Set(ScorerReasonVariableName, reason);
                    }
                }
                else
                {
                    this._logger.LogWarning("Scorer instruction is empty. Returning false.");
                }
            }
        }
        else
        {
            scorerResult = false;
        }

        if (scorerResult)
        {
            this._prometheusTelemetryService.RecordMetric(MetricName.SuccessAutoScorerCounter, 1);
            await this._ctfdFlagSubmissionService.SubmitFlagAsync(chatId);
        }
        else
        {
            this._prometheusTelemetryService.RecordMetric(MetricName.FailureAutoScorerCounter, 1);
        }

        context.Variables.Set(ScorerVariableName, scorerResult.ToString());
        this._logger.LogInformation("Scorer output: {Success}", scorerResult);
        return context;
    }

    private sealed class MetapromptData
    {
        public string Goal { get; set; } = "";
        public string Message { get; set; } = "";
    }
    private string GetMetaprompt(string goal, string message)
    {
        //We make sure that keywords are removed from the message.
        var sanitizedMessage = message
            .Replace(ScorerKeywordTrue, "", StringComparison.OrdinalIgnoreCase)
            .Replace(ScorerKeywordFalse, "", StringComparison.OrdinalIgnoreCase);

        var metapromptData = new MetapromptData
        {
            Goal = goal,
            Message = sanitizedMessage
        };
        var data = JsonConvert.SerializeObject(metapromptData);

        return this._challengeOptions.Scorer!.Metaprompt
            .Replace(ContentPlaceholder, data, StringComparison.OrdinalIgnoreCase);
    }
}
