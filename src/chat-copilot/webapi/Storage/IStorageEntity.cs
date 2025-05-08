// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace CopilotChat.WebApi.Storage;

public interface IStorageEntity : ICloneable
{
    /// <summary>
    /// Unique ID of the entity.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Partition key value.
    /// </summary>
    string Partition { get; }
}
