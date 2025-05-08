// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CopilotChat.Shared.Ocr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;

namespace CopilotChat.Shared;

/// <summary>
/// Dependency injection for kernel memory using custom OCR configuration defined in appsettings.json
/// </summary>
public static class MemoryClientBuilderExtensions
{
    public static KernelMemoryBuilder WithCustomOcr(this KernelMemoryBuilder builder, IServiceProvider sp, IConfiguration configuration)
    {
        var ocrEngine = configuration.CreateCustomOcr(sp);

        if (ocrEngine != null)
        {
            builder.WithCustomImageOcr(ocrEngine);
        }

        return builder;
    }
}
