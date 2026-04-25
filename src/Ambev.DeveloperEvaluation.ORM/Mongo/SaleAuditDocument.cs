namespace Ambev.DeveloperEvaluation.ORM.Mongo;

public sealed class SaleAuditDocument
{
    public string Id { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public string NumeroVenda { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset OcorreuEm { get; set; }
}