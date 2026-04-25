using System.Diagnostics.Metrics;
using Ambev.DeveloperEvaluation.Common.Resilience;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace Ambev.DeveloperEvaluation.IoC.Resilience;

public sealed class PollyIntegrationResilienceExecutor : IIntegrationResilienceExecutor
{
    private static readonly Meter Meter = new("Ambev.DeveloperEvaluation.Resilience", "1.0.0");
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>("developer_evaluation_resilience_retries_total");
    private static readonly Counter<long> TimeoutCounter = Meter.CreateCounter<long>("developer_evaluation_resilience_timeouts_total");
    private static readonly Counter<long> CircuitOpenedCounter = Meter.CreateCounter<long>("developer_evaluation_resilience_circuit_opened_total");

    private readonly ILogger<PollyIntegrationResilienceExecutor> _logger;
    private readonly IReadOnlyDictionary<string, ResiliencePipeline> _pipelines;

    public PollyIntegrationResilienceExecutor(ILogger<PollyIntegrationResilienceExecutor> logger)
    {
        _logger = logger;
        _pipelines = new Dictionary<string, ResiliencePipeline>(StringComparer.Ordinal)
        {
            [IntegrationResiliencePipelineNames.MongoAuditWrite] = CreatePipeline(IntegrationResiliencePipelineNames.MongoAuditWrite, TimeSpan.FromSeconds(5)),
            [IntegrationResiliencePipelineNames.RabbitMqPublish] = CreatePipeline(IntegrationResiliencePipelineNames.RabbitMqPublish, TimeSpan.FromSeconds(10)),
            [IntegrationResiliencePipelineNames.RabbitMqSubscribe] = CreatePipeline(IntegrationResiliencePipelineNames.RabbitMqSubscribe, TimeSpan.FromSeconds(10))
        };
    }

    public ValueTask ExecuteAsync(string pipelineName, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default)
    {
        if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
        {
            throw new InvalidOperationException($"O pipeline de resiliência '{pipelineName}' não foi configurado.");
        }

        return pipeline.ExecuteAsync(action, cancellationToken);
    }

    private ResiliencePipeline CreatePipeline(string pipelineName, TimeSpan timeout)
    {
        var builder = new ResiliencePipelineBuilder();

        builder.AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            MaxDelay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            OnRetry = arguments =>
            {
                RetryCounter.Add(1, KeyValuePair.Create<string, object?>("pipeline", pipelineName));
                _logger.LogWarning(
                    arguments.Outcome.Exception,
                    "Retry {Attempt} acionado no pipeline {PipelineName} com atraso de {DelayMs}ms.",
                    arguments.AttemptNumber + 1,
                    pipelineName,
                    arguments.RetryDelay.TotalMilliseconds);

                return default;
            }
        });

        builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            FailureRatio = 0.5,
            MinimumThroughput = 4,
            SamplingDuration = TimeSpan.FromSeconds(20),
            BreakDuration = TimeSpan.FromSeconds(15),
            OnOpened = arguments =>
            {
                CircuitOpenedCounter.Add(1, KeyValuePair.Create<string, object?>("pipeline", pipelineName));
                _logger.LogError(
                    arguments.Outcome.Exception,
                    "Circuit breaker aberto no pipeline {PipelineName} por {BreakDurationMs}ms.",
                    pipelineName,
                    arguments.BreakDuration.TotalMilliseconds);

                return default;
            },
            OnClosed = _ =>
            {
                _logger.LogInformation("Circuit breaker fechado no pipeline {PipelineName}.", pipelineName);
                return default;
            },
            OnHalfOpened = _ =>
            {
                _logger.LogInformation("Circuit breaker em half-open no pipeline {PipelineName}.", pipelineName);
                return default;
            }
        });

        builder.AddTimeout(new TimeoutStrategyOptions
        {
            Timeout = timeout,
            OnTimeout = arguments =>
            {
                TimeoutCounter.Add(1, KeyValuePair.Create<string, object?>("pipeline", pipelineName));
                _logger.LogError(
                    "Timeout acionado no pipeline {PipelineName} após {TimeoutMs}ms.",
                    pipelineName,
                    arguments.Timeout.TotalMilliseconds);

                return default;
            }
        });

        return builder.Build();
    }
}