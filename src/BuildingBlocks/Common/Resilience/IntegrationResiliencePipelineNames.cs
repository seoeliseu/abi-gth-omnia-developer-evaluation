namespace Ambev.DeveloperEvaluation.Common.Resilience;

public static class IntegrationResiliencePipelineNames
{
    public const string MongoAuditWrite = "mongo-audit-write";
    public const string RabbitMqPublish = "rabbitmq-publish";
    public const string RabbitMqSubscribe = "rabbitmq-subscribe";
}