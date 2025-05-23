﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// Defines the basic CRUD operations for a storage context.
/// </summary>
public interface IStorageContext<T> where T : IStorageEntity
{
    /// <summary>
    /// Query entities in the storage context.
    /// </summary>
    Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate);

    /// <summary>
    /// Read an entity from the storage context by id.
    /// </summary>
    /// <param name="entityId">The entity id.</param>
    /// <param name="partitionKey">The entity partition</param>
    /// <returns>The entity.</returns>
    Task<T> ReadAsync(string entityId, string partitionKey);

    /// <summary>
    /// Create an entity in the storage context.
    /// </summary>
    /// <param name="entity">The entity to be created in the context.</param>
    Task CreateAsync(T entity);

    /// <summary>
    /// Upsert an entity in the storage context.
    /// </summary>
    /// <param name="entity">The entity to be upserted in the context.</param>
    Task UpsertAsync(T entity);

    /// <summary>
    /// Delete an entity from the storage context.
    /// </summary>
    /// <param name="entity">The entity to be deleted from the context.</param>
    Task DeleteAsync(T entity);
}
