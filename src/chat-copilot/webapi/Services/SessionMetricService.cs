// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using CopilotChat.WebApi.Models;

namespace CopilotChat.WebApi.Services;

public class SessionMetricService : ISessionMetricService
{
    private readonly IPrometheusTelemetryService _prometheusTelemetryService;
    private readonly Dictionary<string, Stopwatch> _sessionLengths;
    private readonly HashSet<string> _userIds;

    public SessionMetricService(IPrometheusTelemetryService prometheusTelemetryService)
    {
        this._prometheusTelemetryService = prometheusTelemetryService;
        this._sessionLengths = new Dictionary<string, Stopwatch>();
        this._userIds = new HashSet<string>();
    }

    public void OnConnected(string connectionId)
    {
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        this._sessionLengths.Add(connectionId, stopWatch);
    }

    public void OnDisconnect(string connectionId)
    {
        if (this._sessionLengths.TryGetValue(connectionId, out var stopWatch))
        {
            stopWatch.Stop();
            var seconds = stopWatch.Elapsed.TotalSeconds;
            this._prometheusTelemetryService.RecordMetric(MetricName.UserSesionSummary, seconds);
            this._sessionLengths.Remove(connectionId);
        }
    }

    public void TrackUserId(string userId)
    {
        if (this._userIds.Contains(userId))
        {
            return;
        }

        this._userIds.Add(userId);
        this._prometheusTelemetryService.RecordMetric(MetricName.UserCounter, 1);
    }
}
