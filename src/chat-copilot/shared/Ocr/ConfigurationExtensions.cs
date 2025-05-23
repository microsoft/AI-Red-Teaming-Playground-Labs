﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using CopilotChat.Shared.Ocr.Tesseract;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats.Image;

namespace CopilotChat.Shared.Ocr;

/// <summary>
/// Dependency injection for kernel memory using configuration defined in appsettings.json
/// </summary>
public static class ConfigurationExtensions
{
    private const string ConfigOcrType = "ImageOcrType";

    public static IOcrEngine? CreateCustomOcr(this IConfiguration configuration, IServiceProvider sp)
    {
        var ocrType = configuration.GetSection($"{MemoryConfiguration.KernelMemorySection}:{ConfigOcrType}").Value ?? string.Empty;
        switch (ocrType)
        {
            case string x when x.Equals(TesseractOptions.SectionName, StringComparison.OrdinalIgnoreCase):
                var tesseractOptions =
                    configuration
                        .GetSection($"{MemoryConfiguration.KernelMemorySection}:{MemoryConfiguration.ServicesSection}:{TesseractOptions.SectionName}")
                        .Get<TesseractOptions>();

                if (tesseractOptions == null)
                {
                    throw new ConfigurationException($"Missing configuration for {ConfigOcrType}: {ocrType}");
                }
                return new TesseractOcrEngine(tesseractOptions, sp.GetRequiredService<ILogger<TesseractOcrEngine>>());

            default: // Allow for fall-through for standard OCR settings
                break;
        }

        return null;
    }
}
