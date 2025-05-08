// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.DataFormats.Image;
using Tesseract;

namespace CopilotChat.Shared.Ocr.Tesseract;

/// <summary>
/// Wrapper for the TesseractEngine within the Tesseract OCR library.
/// </summary>
public class TesseractOcrEngine : IOcrEngine
{
    private readonly TesseractEngine _engine;
    private readonly ILogger<TesseractOcrEngine> _logger;

    /// <summary>
    /// Creates a new instance of the TesseractEngineWrapper passing in a valid TesseractEngine.
    /// </summary>
    public TesseractOcrEngine(TesseractOptions tesseractOptions, ILogger<TesseractOcrEngine> logger)
    {
        this._engine = new TesseractEngine(tesseractOptions.FilePath, tesseractOptions.Language);
        this._logger = logger;
    }

    ///<inheritdoc/>
    public async Task<string> ExtractTextFromImageAsync(Stream imageContent, CancellationToken cancellationToken = default)
    {
        await using (var imgStream = new MemoryStream())
        {
            await imageContent.CopyToAsync(imgStream);
            imgStream.Position = 0;

            using var img = Pix.LoadFromMemory(imgStream.ToArray());

            using var page = this._engine.Process(img);
            var text = page.GetText();
            this._logger.LogInformation($"Extracted text: {text}");
            return text;

        }
    }
}
