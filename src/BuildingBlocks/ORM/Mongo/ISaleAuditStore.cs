using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Sales.Domain.Entities;

namespace Ambev.DeveloperEvaluation.ORM.Mongo;

public interface ISaleAuditStore
{
    Task RegistrarAsync(Sale sale, IReadOnlyCollection<IDomainEvent> eventos, CancellationToken cancellationToken);
}