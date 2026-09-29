// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core.Pipeline;
using Microsoft.KernelMemory.AI;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Kernel Memory text generation backed by the same OpenAI-compatible endpoint as Semantic Kernel.
/// </summary>
internal sealed class OpenAICompatibleTextGeneration : ITextGeneration
{
    private readonly OpenAIClient _client;
    private readonly string _model;

    public OpenAICompatibleTextGeneration(string apiKey, string model, HttpClient httpClient)
    {
        var options = new OpenAIClientOptions
        {
            Transport = new HttpClientTransport(httpClient),
        };
        this._client = new OpenAIClient(apiKey, options);
        this._model = model;
    }

    public async IAsyncEnumerable<string> GenerateTextAsync(
        string prompt,
        TextGenerationOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new ChatCompletionsOptions
        {
            MaxTokens = options.MaxTokens,
            Temperature = (float)options.Temperature,
            NucleusSamplingFactor = (float)options.TopP,
            FrequencyPenalty = (float)options.FrequencyPenalty,
            PresencePenalty = (float)options.PresencePenalty,
        };
        foreach (var stop in options.StopSequences)
        {
            request.StopSequences.Add(stop);
        }
        request.Messages.Add(new ChatMessage(ChatRole.System, prompt));

        Response<StreamingChatCompletions> response = await this._client
            .GetChatCompletionsStreamingAsync(this._model, request, cancellationToken)
            .ConfigureAwait(false);
        await foreach (var choice in response.Value.GetChoicesStreaming(cancellationToken).ConfigureAwait(false))
        {
            await foreach (var message in choice.GetMessageStreaming(cancellationToken).ConfigureAwait(false))
            {
                yield return message.Content;
            }
        }
    }
}
