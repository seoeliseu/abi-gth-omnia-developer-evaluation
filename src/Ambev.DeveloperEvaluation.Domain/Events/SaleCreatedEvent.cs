using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Domain.Events;

public sealed record SaleCreatedEvent(Guid SaleId, string NumeroVenda) : IDomainEvent
{
    public DateTimeOffset OcorreuEm { get; init; } = DateTimeOffset.UtcNow;
}