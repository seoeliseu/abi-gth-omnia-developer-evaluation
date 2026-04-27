using Ambev.DeveloperEvaluation.Domain.Common;

namespace Ambev.DeveloperEvaluation.Sales.Domain.Events;

public sealed record ItemCancelledEvent(Guid SaleId, Guid SaleItemId, long ProductId) : IDomainEvent
{
    public DateTimeOffset OcorreuEm { get; init; } = DateTimeOffset.UtcNow;
}