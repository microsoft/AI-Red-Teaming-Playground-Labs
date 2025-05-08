// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Services;

public interface ISessionMetricService
{
    public void OnConnected(string connectionId);
    public void OnDisconnect(string connectionId);

    public void TrackUserId(string userId);
}
