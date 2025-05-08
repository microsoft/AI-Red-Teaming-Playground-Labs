// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CopilotChat.WebApi.Models.Response;

namespace CopilotChat.WebApi.Models.Request;

public class ExecutePlanParameters : Ask
{
    public ProposedPlan? Plan { get; set; }
}
