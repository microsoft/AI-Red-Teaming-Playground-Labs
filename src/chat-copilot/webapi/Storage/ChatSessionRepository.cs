// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for chat sessions.
/// </summary>
public class ChatSessionRepository : Repository<ChatSession>
{
    /// <summary>
    /// Initializes a new instance of the ChatSessionRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    /// <param name="memoryCache">The memory cache.</param>
    public ChatSessionRepository(IStorageContext<ChatSession> storageContext, IMemoryCache memoryCache)
        : base(storageContext, memoryCache)
    {
    }

    /// <summary>
    /// Retrieves all chat sessions.
    /// </summary>
    /// <returns>A list of ChatMessages.</returns>
    public async Task<IEnumerable<ChatSession>> GetAllChatsAsync()
    {
        var chats = await base.StorageContext.QueryEntitiesAsync(e => true);
        return chats.Where(x => x.IsDeleted = false);
    }
}
