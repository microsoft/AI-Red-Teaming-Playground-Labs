// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Auth;

/// <summary>
/// Types of authentication used in the system.
/// </summary>
public enum AuthType
{
    /// <summary>
    /// No authentication is required. The user is assigned a GUID in the cookie and is identified by that.
    /// </summary>
    None,

    /// <summary>
    /// Authentication is performed by CTFd. The platform validates the CTFd cookie and checks with the CTFd redis instance if the session exists.
    /// </summary>
    CTFd,

    /// <summary>
    /// Authentication is performed by the challenge home service. The cookie is validated by the instance.
    /// </summary>
    ChallengeHome
}
