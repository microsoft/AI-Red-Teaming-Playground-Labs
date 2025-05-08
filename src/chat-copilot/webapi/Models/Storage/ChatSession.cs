// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Models.Storage;

/// <summary>
/// A chat session
/// </summary>
public class ChatSession : IStorageEntity
{
    private const string CurrentVersion = "2.0";

    /// <summary>
    /// Chat ID that is persistent and unique.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Title of the chat.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Timestamp of the chat creation.
    /// </summary>
    public DateTimeOffset CreatedOn { get; set; }

    /// <summary>
    /// System description of the chat that is used to generate responses.
    /// </summary>
    public string SystemDescription { get; set; }

    /// <summary>
    /// Fixed system description with "TimeSkill" replaced by "TimePlugin"
    /// </summary>
    public string SafeSystemDescription => this.SystemDescription.Replace("TimeSkill", "TimePlugin", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// The balance between long term memory and working term memory.
    /// The higher this value, the more the system will rely on long term memory by lowering
    /// the relevance threshold of long term memory and increasing the threshold score of working memory.
    /// </summary>
    public float MemoryBalance { get; set; } = 0.5F;

    /// <summary>
    /// A list of enabled plugins.
    /// </summary>
    public HashSet<string> EnabledPlugins { get; set; } = new();

    /// <summary>
    /// Used to determine if the current chat requires upgrade.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The partition key for the session.
    /// </summary>
    [JsonIgnore]
    public string Partition => this.Id;

    /// <summary>
    /// Determines if the chat is deleted.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Determines if the chat is locked while in manual scoring.
    /// </summary>
    [JsonPropertyName("locked")]
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Determines if the chat is locked after x number of turns.
    /// </summary>
    [JsonPropertyName("maxTurnReached")]
    public bool MaxTurnReached { get; set; } = false;

    /// <summary>
    /// Determines if the chat already triggered the XSS vulnreability.
    /// </summary>
    public bool XssTriggered { get; set; } = false;

    /// <summary>
    /// Rag document supplied by the user depending on the challenge settings.
    /// </summary>
    public string RagDocument { get; set; } = string.Empty;

    /// <summary>
    /// Rag user input if the RAG is set to read only.
    /// </summary>
    public string RagUserInput { get; set; } = string.Empty;

    /// <summary>
    /// Session data for Ctfd Auth API. Used when the ctfd integration is enabled.
    /// </summary>
    public CtfdAuthApi? CtfdAuth { get; set; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatSession"/> class.
    /// </summary>
    /// <param name="title">The title of the chat.</param>
    /// <param name="systemDescription">The system description of the chat.</param>
    public ChatSession(string title, string systemDescription)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Title = title;
        this.CreatedOn = DateTimeOffset.Now;
        this.SystemDescription = systemDescription;
        this.Version = CurrentVersion;
    }

    public object Clone()
    {
        return new ChatSession(this.Title, this.SystemDescription)
        {
            Id = this.Id,
            CreatedOn = this.CreatedOn,
            MemoryBalance = this.MemoryBalance,
            EnabledPlugins = new HashSet<string>(this.EnabledPlugins),
            Version = this.Version,
            IsDeleted = this.IsDeleted,
            IsLocked = this.IsLocked,
            MaxTurnReached = this.MaxTurnReached,
            XssTriggered = this.XssTriggered,
            RagDocument = this.RagDocument,
            RagUserInput = this.RagUserInput,
            CtfdAuth = this.CtfdAuth != null ? new CtfdAuthApi()
            {
                Cookie = this.CtfdAuth.Cookie,
                Nonce = this.CtfdAuth.Nonce
            } : null
        };
    }
}
