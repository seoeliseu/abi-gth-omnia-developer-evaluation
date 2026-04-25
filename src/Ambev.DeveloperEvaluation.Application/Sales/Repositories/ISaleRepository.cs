using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.Repositories;

public interface ISaleRepository
{
    Task AdicionarAsync(Sale sale, CancellationToken cancellationToken);
    Task AtualizarAsync(Sale sale, CancellationToken cancellationToken);
    Task<Sale?> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Sale>> ListarAsync(CancellationToken cancellationToken);
}