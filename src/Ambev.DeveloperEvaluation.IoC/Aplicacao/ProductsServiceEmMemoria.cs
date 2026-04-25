using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class ProductsServiceEmMemoria : IProductsService
{
    public Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (produtoId <= 0)
        {
            return Task.FromResult(Result<ProductReference>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]));
        }

        return Task.FromResult(Result<ProductReference>.Success(CriarProduto(produtoId)));
    }

    public Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var produtos = produtosIds
            .Where(id => id > 0)
            .Distinct()
            .Select(CriarProduto)
            .ToArray();

        return Task.FromResult(Result<IReadOnlyCollection<ProductReference>>.Success(produtos));
    }

    private static ProductReference CriarProduto(long produtoId)
    {
        var preco = decimal.Round((produtoId * 7.35m) + 10m, 2, MidpointRounding.AwayFromZero);
        var categoria = produtoId % 2 == 0 ? "beverage" : "snack";
        return new ProductReference(produtoId, $"Produto {produtoId}", preco, categoria, true);
    }
}