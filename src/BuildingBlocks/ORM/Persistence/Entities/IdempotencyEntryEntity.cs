namespace Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

public sealed class IdempotencyEntryEntity
{
    public long Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string ResultType { get; set; } = string.Empty;
    public string ResultPayload { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}