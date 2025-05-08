// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// Defines the basic CRUD operations for a repository.
/// </summary>
public class Repository<T> : IRepository<T> where T : IStorageEntity
{
    protected const int DefaultCacheDuration = 120;

    /// <summary>
    /// The memory cache.
    /// </summary>
    protected IMemoryCache MemoryCache { get; private set; }

    /// <summary>
    /// The storage context.
    /// </summary>
    protected IStorageContext<T> StorageContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    public Repository(IStorageContext<T> storageContext, IMemoryCache memoryCache)
    {
        this.StorageContext = storageContext;
        this.MemoryCache = memoryCache;
    }

    /// <inheritdoc/>
    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity ID cannot be null or empty.");
        }

        this.MemoryCache.Set(this.KeyMemoryCache(entity.Id, entity.Partition), entity.Clone(), TimeSpan.FromSeconds(DefaultCacheDuration));

        return this.StorageContext.CreateAsync(entity);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        this.MemoryCache.Remove(this.KeyMemoryCache(entity.Id, entity.Partition));
        return this.StorageContext.DeleteAsync(entity);
    }

    /// <inheritdoc/>
    public async Task<T> FindByIdAsync(string id, string? partition = null)
    {
        if (this.MemoryCache.TryGetValue(this.KeyMemoryCache(id, partition ?? id), out T cached))
        {
            return cached;
        }
        var result = await this.StorageContext.ReadAsync(id, partition ?? id);
        this.MemoryCache.Set(this.KeyMemoryCache(id, partition ?? id), result.Clone(), TimeSpan.FromSeconds(DefaultCacheDuration));

        return result;
    }

    /// <inheritdoc/>
    public async Task<bool> TryFindByIdAsync(string id, string? partition = null, Action<T?>? callback = null)
    {
        try
        {
            T? found = await this.FindByIdAsync(id, partition ?? id);

            callback?.Invoke(found);

            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is KeyNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        this.MemoryCache.Set(this.KeyMemoryCache(entity.Id, entity.Partition), entity.Clone(), TimeSpan.FromSeconds(DefaultCacheDuration));
        return this.StorageContext.UpsertAsync(entity);
    }

    private string KeyMemoryCache(string id, string partition)
    {
        return $"r|{id}|{partition}";
    }
}
