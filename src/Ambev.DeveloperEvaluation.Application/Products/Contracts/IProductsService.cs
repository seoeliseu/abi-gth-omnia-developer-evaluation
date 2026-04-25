using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Products.Contracts;

public interface IProductsService
{
    Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken);
    Task<Result<PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken);
    Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken);
    Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken);
    Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken);
    Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken);
    Task<Result<PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken);
}