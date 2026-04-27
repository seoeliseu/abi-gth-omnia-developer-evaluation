using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Sales.Application.Contracts;
using Ambev.DeveloperEvaluation.Sales.Application.Repositories;
using Ambev.DeveloperEvaluation.Sales.Application.Services;
using Ambev.DeveloperEvaluation.Sales.Domain.Entities;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;

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

    [Fact]
    public async Task Deve_remover_venda_existente()
    {
        var service = CriarServico();
        var criacao = await service.CriarAsync(
            new CreateSaleRequest("VEN-DELETE", DateTimeOffset.UtcNow, 1, 10, "Filial 1", [new CreateSaleItemRequest(1, 2)]),
            null,
            CancellationToken.None);

        var remocao = await service.RemoverAsync(criacao.Value!.Id, CancellationToken.None);
        var consulta = await service.ObterPorIdAsync(criacao.Value.Id, CancellationToken.None);

        Assert.True(remocao.IsSuccess);
        Assert.True(consulta.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, consulta.ErrorType);
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

        public Task RemoverAsync(Sale sale, CancellationToken cancellationToken)
        {
            _sales.Remove(sale.Id);
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

        public Task<Result<Ambev.DeveloperEvaluation.Users.Application.Common.PagedResult<UserDetail>>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<UserDetail> usuarios =
            [
                new UserDetail(1, "email@example.com", "usuario1", "123456", new UserNameData("Usuario", "Um"), new UserAddressData("São Paulo", "Rua A", 1, "01000-000", new UserGeolocationData("-23.55", "-46.63")), "11999999999", "Active", "Customer")
            ];

            var paged = new Ambev.DeveloperEvaluation.Users.Application.Common.PagedResult<UserDetail>(usuarios, usuarios.Count, 1, 1);
            return Task.FromResult(Result<Ambev.DeveloperEvaluation.Users.Application.Common.PagedResult<UserDetail>>.Success(paged));
        }

        public Task<Result<UserDetail>> ObterDetalhePorIdAsync(long usuarioId, CancellationToken cancellationToken)
        {
            var usuario = new UserDetail(usuarioId, "email@example.com", $"usuario{usuarioId}", "123456", new UserNameData("Usuario", "Teste"), new UserAddressData("São Paulo", "Rua A", 1, "01000-000", new UserGeolocationData("-23.55", "-46.63")), "11999999999", "Active", "Customer");
            return Task.FromResult(Result<UserDetail>.Success(usuario));
        }

        public Task<Result<UserDetail>> CriarAsync(UpsertUserRequest requisicao, CancellationToken cancellationToken)
        {
            var usuario = new UserDetail(99, requisicao.Email, requisicao.Username, requisicao.Password, requisicao.Name, requisicao.Address, requisicao.Phone, requisicao.Status, requisicao.Role);
            return Task.FromResult(Result<UserDetail>.Success(usuario));
        }

        public Task<Result<UserDetail>> AtualizarAsync(long usuarioId, UpsertUserRequest requisicao, CancellationToken cancellationToken)
        {
            var usuario = new UserDetail(usuarioId, requisicao.Email, requisicao.Username, requisicao.Password, requisicao.Name, requisicao.Address, requisicao.Phone, requisicao.Status, requisicao.Role);
            return Task.FromResult(Result<UserDetail>.Success(usuario));
        }

        public Task<Result<UserDetail>> RemoverAsync(long usuarioId, CancellationToken cancellationToken)
        {
            return ObterDetalhePorIdAsync(usuarioId, cancellationToken);
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

        public Task<Result<Ambev.DeveloperEvaluation.Products.Application.Common.PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ProductDetail> produtos =
            [
                new ProductDetail(1, "Produto 1", 10m, "Descricao", "categoria", "https://example.com/1.png", new ProductRatingData(4.5m, 10), true)
            ];

            return Task.FromResult(Result<Ambev.DeveloperEvaluation.Products.Application.Common.PagedResult<ProductDetail>>.Success(new Ambev.DeveloperEvaluation.Products.Application.Common.PagedResult<ProductDetail>(produtos, produtos.Count, 1, 1)));
        }

        public Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken)
        {
            var produto = new ProductDetail(99, requisicao.Title, requisicao.Price, requisicao.Description, requisicao.Category, requisicao.Image, requisicao.Rating, true);
            return Task.FromResult(Result<ProductDetail>.Success(produto));
        }

        public Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken)
        {
            var produto = new ProductDetail(produtoId, requisicao.Title, requisicao.Price, requisicao.Description, requisicao.Category, requisicao.Image, requisicao.Rating, true);
            return Task.FromResult(Result<ProductDetail>.Success(produto));
        }

        public Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken)
        {
            var produto = new ProductDetail(produtoId, $"Produto {produtoId}", 10m, "Descricao", "categoria", "https://example.com/1.png", new ProductRatingData(4.5m, 10), true);
            return Task.FromResult(Result<ProductDetail>.Success(produto));
        }

        public Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken)
        {
            return ObterDetalhePorIdAsync(produtoId, cancellationToken);
        }

        public Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken)
        {
            IReadOnlyCollection<string> categorias = ["categoria"];
            return Task.FromResult(Result<IReadOnlyCollection<string>>.Success(categorias));
        }

        public Task<Result<Ambev.DeveloperEvaluation.Products.Application.Common.PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
        {
            return ListarAsync(filtro with { Category = categoria }, cancellationToken);
        }
    }

    private sealed class FakeIdempotencyStore : IIdempotencyStore
    {
        private readonly Dictionary<string, RegistroIdempotencia> _entradas = [];

        public bool TryGet<T>(string escopo, string chave, out IdempotencyEntry<T>? entrada)
        {
            var existe = _entradas.TryGetValue($"{escopo}:{chave}", out var valor);
            if (existe && valor is not null && valor.Resultado is T resultadoTipado)
            {
                entrada = new IdempotencyEntry<T>(valor.Fingerprint, resultadoTipado, valor.CriadoEm);
                return true;
            }

            entrada = null;
            return false;
        }

        public void Set<T>(string escopo, string chave, string fingerprint, T resultado)
        {
            _entradas[$"{escopo}:{chave}"] = new RegistroIdempotencia(fingerprint, resultado, DateTimeOffset.UtcNow);
        }

        private sealed record RegistroIdempotencia(string Fingerprint, object? Resultado, DateTimeOffset CriadoEm);
    }
}