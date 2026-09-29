// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Route the OpenAI SDK's /v1 requests to an OpenAI-compatible API.
/// </summary>
internal sealed class OpenAIEndpointHandler : DelegatingHandler
{
    private readonly Uri _baseUri;

    public OpenAIEndpointHandler(Uri baseUri)
    {
        this._baseUri = baseUri;
    }

    public static Uri ParseBaseUrl(string baseUrl)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("The OpenAI-compatible base URL must be an HTTPS URL without credentials, query, or fragment.", nameof(baseUrl));
        }

        int authorityStart = baseUrl.IndexOf("://", StringComparison.Ordinal) + 3;
        int pathStart = baseUrl.IndexOf('/', authorityStart);
        int authorityLength = (pathStart < 0 ? baseUrl.Length : pathStart) - authorityStart;

        if (baseUrl.Contains('?', StringComparison.Ordinal) ||
            baseUrl.Contains('#', StringComparison.Ordinal) ||
            baseUrl.Contains('\\', StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(uri.UserInfo) ||
            baseUrl.IndexOf('@', authorityStart, authorityLength) >= 0)
        {
            throw new ArgumentException("The OpenAI-compatible base URL must be an HTTPS URL without credentials, query, or fragment.", nameof(baseUrl));
        }

        return new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var source = request.RequestUri;
        if (source is null || source.Scheme != Uri.UriSchemeHttps || source.Host != "api.openai.com" ||
            !source.IsDefaultPort || !string.IsNullOrEmpty(source.UserInfo) ||
            !source.AbsolutePath.StartsWith("/v1/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unexpected OpenAI SDK endpoint; refusing to forward an authenticated request.");
        }

        request.RequestUri = new Uri(this._baseUri, string.Concat(source.AbsolutePath.AsSpan("/v1/".Length), source.Query.AsSpan()));
        return base.SendAsync(request, cancellationToken);
    }
}
