using Ambev.DeveloperEvaluation.Sales.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Sales.Application.Repositories;

public interface ISaleRepository
{
    Task AdicionarAsync(Sale sale, CancellationToken cancellationToken);
    Task AtualizarAsync(Sale sale, CancellationToken cancellationToken);
    Task<Sale?> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sale>> ListarAsync(CancellationToken cancellationToken);
}