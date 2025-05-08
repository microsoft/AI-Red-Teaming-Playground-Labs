// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.AspNetCore.Authorization;

namespace CopilotChat.WebApi.Auth;

/// <summary>
/// Used to require the chat to be owned by the authenticated user.
/// </summary>
public class ChatParticipantRequirement : IAuthorizationRequirement
{
}
