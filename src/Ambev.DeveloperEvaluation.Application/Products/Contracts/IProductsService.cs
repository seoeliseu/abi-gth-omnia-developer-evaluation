using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Products.Contracts;

public interface IProductsService
{
    Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken);
}