using Ambev.DeveloperEvaluation.Products.Application.Common;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Products.Application.Repositories;

public interface IProductRepository
{
    Task<Product?> ObterAtivoPorIdAsync(long produtoId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Product>> ListarAtivosPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken);
    Task<PagedResult<Product>> ListarAtivosAsync(ProductListFilter filtro, CancellationToken cancellationToken);
    Task<PagedResult<Product>> ListarAtivosPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> ListarCategoriasAtivasAsync(CancellationToken cancellationToken);
    Task<Product> AdicionarAsync(Product produto, CancellationToken cancellationToken);
    Task AtualizarAsync(Product produto, CancellationToken cancellationToken);
}
