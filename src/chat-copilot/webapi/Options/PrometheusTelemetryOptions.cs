// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Options;

public class PrometheusTelemetryOptions
{
    public const string PropertyName = "PrometheusTelemetry";

    public string Endpoint { get; set; } = "http://localhost:4001";
}
