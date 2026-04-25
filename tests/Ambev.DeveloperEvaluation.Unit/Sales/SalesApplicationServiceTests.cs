using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Repositories;
using Ambev.DeveloperEvaluation.Application.Sales.Services;
using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Unit.Sales;

public class SalesApplicationServiceTests
{
    [Fact]
    public async Task Deve_criar_venda_e_reaproveitar_resultado_idempotente()
    {
        var service = CriarServico();
        var requisicao = new CreateSaleRequest(
            "VEN-1000",
            new DateTimeOffset(2026, 4, 25, 10, 0, 0, TimeSpan.Zero),
            1,
            10,
            "Filial Centro",
            [new CreateSaleItemRequest(10, 4)]);

        var resultadoA = await service.CriarAsync(requisicao, "idem-1", CancellationToken.None);
        var resultadoB = await service.CriarAsync(requisicao, "idem-1", CancellationToken.None);

        Assert.True(resultadoA.IsSuccess);
        Assert.True(resultadoB.IsSuccess);
        Assert.Equal(resultadoA.Value?.Id, resultadoB.Value?.Id);
    }

    [Fact]
    public async Task Deve_listar_vendas_com_paginacao()
    {
        var service = CriarServico();

        await service.CriarAsync(new CreateSaleRequest("VEN-1", DateTimeOffset.UtcNow, 1, 10, "Filial 1", [new CreateSaleItemRequest(1, 2)]), null, CancellationToken.None);
        await service.CriarAsync(new CreateSaleRequest("VEN-2", DateTimeOffset.UtcNow.AddMinutes(1), 2, 10, "Filial 1", [new CreateSaleItemRequest(2, 4)]), null, CancellationToken.None);

        var resultado = await service.ListarAsync(new SaleListFilter(Page: 1, Size: 1, Order: "dataVenda desc"), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);
        Assert.Equal(2, resultado.Value!.TotalItems);
        Assert.Single(resultado.Value.Data);
    }

    private static ISalesApplicationService CriarServico()
    {
        return new SalesApplicationService(
            new FakeSaleRepository(),
            new FakeUsersService(),
            new FakeProductsService(),
            new FakeIdempotencyStore());
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        private readonly Dictionary<Guid, Sale> _sales = [];

        public Task AdicionarAsync(Sale sale, CancellationToken cancellationToken)
        {
            _sales[sale.Id] = sale;
            return Task.CompletedTask;
        }

        public Task AtualizarAsync(Sale sale, CancellationToken cancellationToken)
        {
            _sales[sale.Id] = sale;
            return Task.CompletedTask;
        }

        public Task<Sale?> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken)
        {
            _sales.TryGetValue(saleId, out var sale);
            return Task.FromResult(sale);
        }

        public Task<IReadOnlyCollection<Sale>> ListarAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult((IReadOnlyCollection<Sale>)_sales.Values.ToArray());
        }
    }

    private sealed class FakeUsersService : IUsersService
    {
        public Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<UserReference>.Success(new UserReference(usuarioId, $"usuario{usuarioId}", "email@example.com", "Active", "Customer", true)));
        }

        public Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<UserReference> usuarios = [new UserReference(1, "usuario1", "email@example.com", "Active", "Customer", true)];
            return Task.FromResult(Result<IReadOnlyCollection<UserReference>>.Success(usuarios));
        }
    }

    private sealed class FakeProductsService : IProductsService
    {
        public Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<ProductReference>.Success(new ProductReference(produtoId, $"Produto {produtoId}", 10m, "categoria", true)));
        }

        public Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ProductReference> produtos = produtosIds.Select(id => new ProductReference(id, $"Produto {id}", 10m, "categoria", true)).ToArray();
            return Task.FromResult(Result<IReadOnlyCollection<ProductReference>>.Success(produtos));
        }
    }

    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<string, IdempotencyEntry> _entradas = [];

        public bool TryGet(string escopo, string chave, out IdempotencyEntry? entrada)
        {
            var existe = _entradas.TryGetValue($"{escopo}:{chave}", out var valor);
            entrada = valor;
            return existe;
        }

        public void Set(string escopo, string chave, string fingerprint, object resultado)
        {
            _entradas[$"{escopo}:{chave}"] = new IdempotencyEntry(fingerprint, resultado, DateTimeOffset.UtcNow);
        }
    }
}