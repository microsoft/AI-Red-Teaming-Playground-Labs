// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Models;

public static class MetricName
{
    /// <summary>
    /// Numbers of token that are used by this particular challenge
    /// </summary>
    public const string TokenCounter = "token";

    //Chat metrics

    /// <summary>
    /// Number of users that are using the chat service
    /// </summary>
    public const string UserCounter = "user";

    /// <summary>
    /// Summary of the average session length on this challenge.
    /// </summary>
    public const string UserSesionSummary = "user_session";

    /// <summary>
    /// Number of messages sent to the bot
    /// </summary>
    public const string MessageCounter = "chat_message_counter";

    /// <summary>
    /// Number of chat sessions
    /// </summary>
    public const string ChatSessionCounter = "chat";

    /// <summary>
    /// Number of chat sessions deleted
    /// </summary>
    public const string ChatSessionDeleteCounter = "chat_deleted";

    //Scoring

    /// <summary>
    /// Number of success from auto scorer includes XSS scorer
    /// </summary>
    public const string SuccessAutoScorerCounter = "success_auto_scorer";

    /// <summary>
    /// Number of success from manual scorer
    /// </summary>
    public const string SuccessManualScorerCounter = "success_manual_scorer";

    /// <summary>
    /// Number of failure from auto scorer
    /// </summary>
    public const string FailureAutoScorerCounter = "failure_auto_scorer";

    /// <summary>
    /// Number of failure from auto scorer
    /// </summary>
    public const string FailureManualScorerCounter = "failure_manual_scorer";

    /// <summary>
    /// Number of requests sent to the manual scorer
    /// </summary>
    public const string ManualScorerCounter = "manual_scorer";
}
