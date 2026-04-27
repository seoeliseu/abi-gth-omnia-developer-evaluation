namespace Ambev.DeveloperEvaluation.Common.Resilience;

public interface IIntegrationResilienceExecutor
{
    ValueTask ExecuteAsync(string pipelineName, Func<CancellationToken, ValueTask> action, CancellationToken cancellationToken = default);
}