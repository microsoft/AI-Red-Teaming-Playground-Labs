// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CopilotChat.WebApi.Models;
using CopilotChat.WebApi.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prometheus;

namespace CopilotChat.WebApi.Services;
public class PrometheusTelemetryService : IPrometheusTelemetryService, IDisposable
{
    private readonly Dictionary<string, ICollectorChild> _metrics;
    private readonly ILogger<PrometheusTelemetryService> _logger;
    private KestrelMetricServer? _server;

    public PrometheusTelemetryService(
        IOptions<PrometheusTelemetryOptions> prometheusOptions,
        IOptions<ChallengeOptions> challengeOptions,
        IHostApplicationLifetime applicationLifetime,
        ILogger<PrometheusTelemetryService> logger)
    {
        var endpoint = prometheusOptions.Value.Endpoint;
        this._logger = logger;
        this._logger.LogInformation("Starting Prometheus Telemetry Service on {Endpoint}", endpoint);

        Metrics.DefaultRegistry.SetStaticLabels(new Dictionary<string, string>
        {
            {"challenge_id", $"{challengeOptions.Value.Id}"},
        });

        Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions
        {
            SuppressEventCounters = true
        });

        var uri = new Uri(endpoint);

        this._server = new Prometheus.KestrelMetricServer(new KestrelMetricServerOptions
        {
            Port = (ushort)uri.Port,
            Hostname = uri.Host
        });
        this._server.Start();

        applicationLifetime.ApplicationStopping.Register(() => this.OnShutdown());

        this._metrics = new Dictionary<string, ICollectorChild>();

        this.Setup();
    }

    private void Setup()
    {
        //Seting up the metrics to be used here
        this.SetupCounter(MetricName.TokenCounter, "tokens", "Number of tokens used");

        this.SetupCounter(MetricName.UserCounter, "users", "Number of users that are using the chat service", "session");

        this.SetupHistogram(MetricName.UserSesionSummary, "seconds", "Summary of the average session length on this challenge", new double[] { 10, 30, 60, 120, 300, 600, 1200, 2400, 4800 }, "session");

        this.SetupCounter(MetricName.MessageCounter, "messages", "Number of messages sent to the bot", "chat");

        this.SetupCounter(MetricName.ChatSessionCounter, "chats", "Number of chat sessions", "chat");

        this.SetupCounter(MetricName.ChatSessionDeleteCounter, "chats", "Number of chat sessions deleted", "chat_deleted");

        this.SetupCounter(MetricName.SuccessAutoScorerCounter, "successes", "Number of success from auto scorer includes XSS scorer", "auto_scorer");

        this.SetupCounter(MetricName.SuccessManualScorerCounter, "successes", "Number of success from manual scorer", "manual_scorer");

        this.SetupCounter(MetricName.FailureAutoScorerCounter, "failures", "Number of failure from auto scorer includes XSS scorer", "auto_scorer");

        this.SetupCounter(MetricName.FailureManualScorerCounter, "failures", "Number of failure from auto scorer", "manual_scorer");

        this.SetupCounter(MetricName.ManualScorerCounter, "evaluations", "Number of manual scorer", "manual_scorer");
    }

    private void SetupCounter(string keyName, string unit, string description, string metricName = "")
    {
        if (string.IsNullOrEmpty(metricName))
        {
            metricName = keyName;
        }

        metricName = $"chat_copilot_{metricName}_{unit}_total";

        var metric = Metrics.CreateCounter(metricName, description);
        this._metrics.Add(keyName, metric);
    }

    private void SetupHistogram(string keyName, string unit, string description, double[] bins, string metricName = "")
    {
        if (string.IsNullOrEmpty(metricName))
        {
            metricName = keyName;
        }

        metricName = $"chat_copilot_{metricName}_{unit}";

        var metric = Metrics.CreateHistogram(metricName, description, new HistogramConfiguration { Buckets = bins });
        this._metrics.Add(keyName, metric);
    }

    /// <summary>
    /// Record a metric
    /// </summary>
    /// <param name="metricName"></param>
    /// <param name="value"></param>
    public void RecordMetric(string metricName, double value)
    {
        if (this._metrics.TryGetValue(metricName, out var metric))
        {
            if (metric is Counter counter)
            {
                counter.Inc(value);
            }
            else if (metric is Histogram histogram)
            {
                histogram.Observe(value);
            }
        }
    }

    public void OnShutdown()
    {
        if (this._server != null)
        {
            this._server.Dispose();
            this._server = null;
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // free managed resources
            this.OnShutdown();
        }
    }

    ~PrometheusTelemetryService()
    {
        this.Dispose(false);
    }
}
