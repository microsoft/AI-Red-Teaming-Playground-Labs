// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel.DataAnnotations;

namespace CopilotChat.WebApi.Options;

/// <summary>
/// File system storage configuration.
/// </summary>
public class FileSystemOptions
{
    /// <summary>
    /// Gets or sets the file path for persistent file system storage.
    /// </summary>
    [Required, NotEmptyOrWhitespace]
    public string FilePath { get; set; } = string.Empty;
}
