// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat participants.
/// </summary>
public class ChatParticipantRepository : Repository<ChatParticipant>
{
    /// <summary>
    /// Initializes a new instance of the ChatParticipantRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    /// <param name="memoryCache">The memory cache.</param>
    public ChatParticipantRepository(IStorageContext<ChatParticipant> storageContext, IMemoryCache memoryCache)
        : base(storageContext, memoryCache)
    {
    }

    /// <summary>
    /// Finds chat participants by user id.
    /// A user can be part of multiple chats, thus a user can have multiple chat participants.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <returns>A list of chat participants of the same user id in different chat sessions.</returns>
    public async Task<IEnumerable<ChatParticipant>> FindByUserIdAsync(string userId)
    {
        //Can't cache this
        var result = await base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId);
        return result;
    }

    /// <summary>
    /// Finds chat participants by chat id.
    /// </summary>
    /// <param name="chatId">The chat id.</param>
    /// <returns>A list of chat participants in the same chat sessions.</returns>
    public async Task<IEnumerable<ChatParticipant>> FindByChatIdAsync(string chatId)
    {
        //We don't really care about what's in the database really since we block the feature of adding new participants.
        //Let's just cache the result for a while to give the db a break.
        var key = this.MemoryKeyChatId(chatId);
        if (this.MemoryCache.TryGetValue(key, out IEnumerable<ChatParticipant> participants))
        {
            return participants;
        }

        var result = await base.StorageContext.QueryEntitiesAsync(e => e.ChatId == chatId);
        var newResult = result.ToList().Select(x => (ChatParticipant)x.Clone());
        this.MemoryCache.Set(key, newResult, TimeSpan.FromSeconds(DefaultCacheDuration));

        return result;
    }

    /// <summary>
    /// Checks if a user is in a chat session.
    /// </summary>
    /// <param name="userId">The user id.</param>
    /// <param name="chatId">The chat id.</param>
    /// <returns>True if the user is in the chat session, false otherwise.</returns>
    public async Task<bool> IsUserInChatAsync(string userId, string chatId)
    {
        //We don't really care about what's in the database really since we block the feature of adding new participants.
        //Let's just cache the result for a while to give the db a break.
        var key = this.MemoryKeyUserChat(userId, chatId);
        if (this.MemoryCache.TryGetValue(key, out bool result))
        {
            return result;
        }
        var users = await base.StorageContext.QueryEntitiesAsync(e => e.UserId == userId && e.ChatId == chatId);
        result = users.Any();

        this.MemoryCache.Set(key, result, TimeSpan.FromSeconds(DefaultCacheDuration));
        return result;
    }

    private string MemoryKeyUserId(string userId) => $"P-UserId_{userId}";
    private string MemoryKeyChatId(string chatId) => $"P-ChatId_{chatId}";
    private string MemoryKeyUserChat(string userId, string chatId) => $"P-{userId}-{chatId}";
}
