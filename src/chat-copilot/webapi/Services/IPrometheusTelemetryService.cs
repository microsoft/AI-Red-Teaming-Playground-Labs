// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CopilotChat.WebApi.Services;

public interface IPrometheusTelemetryService
{
    /// <summary>
    /// Records a metric with a given value.
    /// </summary>
    /// <param name="metricName"></param>
    /// <param name="value"></param>
    public void RecordMetric(string metricName, double value);
}
