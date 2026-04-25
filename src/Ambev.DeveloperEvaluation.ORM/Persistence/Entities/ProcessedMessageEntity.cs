namespace Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

public sealed class ProcessedMessageEntity
{
    public Guid Id { get; set; }
    public string Consumer { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
}