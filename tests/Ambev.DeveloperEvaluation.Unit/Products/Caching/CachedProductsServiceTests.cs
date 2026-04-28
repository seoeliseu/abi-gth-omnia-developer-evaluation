using System.Text;
using Ambev.DeveloperEvaluation.Products.Application.Common;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Repositories;
using Ambev.DeveloperEvaluation.Products.Application.Services;
using Ambev.DeveloperEvaluation.Products.Domain.Entities;
using Ambev.DeveloperEvaluation.Products.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Products.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ambev.DeveloperEvaluation.Unit.Products.Caching;

public sealed class CachedProductsServiceTests
{
    [Fact]
    public async Task ObterDetalhePorIdAsync_DeveUsarCacheNaSegundaConsulta()
    {
        var repository = new InMemoryProductRepository([CriarProduto(1, "Produto 1", "categoria-a")]);
        var sut = CriarSut(repository);

        var primeiroResultado = await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);
        var segundoResultado = await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);

        Assert.True(primeiroResultado.IsSuccess);
        Assert.True(segundoResultado.IsSuccess);
        Assert.Equal(1, repository.ObterAtivoPorIdCalls);
        Assert.Equal("Produto 1", segundoResultado.Value!.Title);
    }

    [Fact]
    public async Task CriarAsync_DeveInvalidarCacheDeListagem()
    {
        var repository = new InMemoryProductRepository([CriarProduto(1, "Produto 1", "categoria-a")]);
        var sut = CriarSut(repository);
        var filtro = new ProductListFilter();

        var primeiraListagem = await sut.ListarAsync(filtro, CancellationToken.None);
        var criacao = await sut.CriarAsync(CriarRequisicao("Produto 2", "categoria-b"), CancellationToken.None);
        var segundaListagem = await sut.ListarAsync(filtro, CancellationToken.None);

        Assert.True(primeiraListagem.IsSuccess);
        Assert.True(criacao.IsSuccess);
        Assert.True(segundaListagem.IsSuccess);
        Assert.Equal(2, repository.ListarAtivosCalls);
        Assert.Equal(2, segundaListagem.Value!.TotalItems);
    }

    [Fact]
    public async Task AtualizarAsync_DeveInvalidarCacheDeDetalhe()
    {
        var repository = new InMemoryProductRepository([CriarProduto(1, "Produto 1", "categoria-a")]);
        var sut = CriarSut(repository);

        await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);
        var atualizacao = await sut.AtualizarAsync(1, CriarRequisicao("Produto Atualizado", "categoria-a", 45.9m), CancellationToken.None);
        var detalheAtualizado = await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);

        Assert.True(atualizacao.IsSuccess);
        Assert.True(detalheAtualizado.IsSuccess);
        Assert.Equal(3, repository.ObterAtivoPorIdCalls);
        Assert.Equal("Produto Atualizado", detalheAtualizado.Value!.Title);
        Assert.Equal(45.9m, detalheAtualizado.Value.Price);
    }

    [Fact]
    public async Task RemoverAsync_DeveInvalidarCacheDeDetalhe()
    {
        var repository = new InMemoryProductRepository([CriarProduto(1, "Produto 1", "categoria-a")]);
        var sut = CriarSut(repository);

        await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);
        var remocao = await sut.RemoverAsync(1, CancellationToken.None);
        var detalheRemovido = await sut.ObterDetalhePorIdAsync(1, CancellationToken.None);

        Assert.True(remocao.IsSuccess);
        Assert.False(detalheRemovido.IsSuccess);
        Assert.Equal(3, repository.ObterAtivoPorIdCalls);
    }

    private static CachedProductsService CriarSut(InMemoryProductRepository repository)
    {
        var inner = new ProductsApplicationService(repository);
        return new CachedProductsService(inner, new InMemoryDistributedCache(), NullLogger<CachedProductsService>.Instance);
    }

    private static Product CriarProduto(long id, string titulo, string categoria)
    {
        return Product.Reidratar(
            id,
            titulo,
            19.99m,
            $"Descricao {titulo}",
            categoria,
            "https://example.com/produto.png",
            new ProductRating(4.2m, 10),
            true);
    }

    private static UpsertProductRequest CriarRequisicao(string titulo, string categoria, decimal preco = 29.99m)
    {
        return new UpsertProductRequest(
            titulo,
            preco,
            $"Descricao {titulo}",
            categoria,
            "https://example.com/produto.png",
            new ProductRatingData(4.7m, 12));
    }

    private sealed class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products;
        private long _nextId;

        public InMemoryProductRepository(IEnumerable<Product> products)
        {
            _products = products.ToList();
            _nextId = _products.Count == 0 ? 1 : _products.Max(product => product.Id) + 1;
        }

        public int ObterAtivoPorIdCalls { get; private set; }
        public int ListarAtivosCalls { get; private set; }

        public Task<Product?> ObterAtivoPorIdAsync(long produtoId, CancellationToken cancellationToken)
        {
            ObterAtivoPorIdCalls++;
            var product = _products.FirstOrDefault(product => product.Id == produtoId && product.Active);
            return Task.FromResult(product);
        }

        public Task<IReadOnlyCollection<Product>> ListarAtivosPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
        {
            var products = _products.Where(product => product.Active && produtosIds.Contains(product.Id)).ToArray();
            return Task.FromResult<IReadOnlyCollection<Product>>(products);
        }

        public Task<PagedResult<Product>> ListarAtivosAsync(ProductListFilter filtro, CancellationToken cancellationToken)
        {
            ListarAtivosCalls++;
            var query = _products.Where(product => product.Active);
            if (!string.IsNullOrWhiteSpace(filtro.Category))
            {
                query = query.Where(product => string.Equals(product.Category, filtro.Category, StringComparison.OrdinalIgnoreCase));
            }

            var items = query.ToArray();
            return Task.FromResult(new PagedResult<Product>(items, items.Length, 1, 1));
        }

        public Task<PagedResult<Product>> ListarAtivosPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
        {
            var items = _products.Where(product => product.Active && string.Equals(product.Category, categoria, StringComparison.OrdinalIgnoreCase)).ToArray();
            return Task.FromResult(new PagedResult<Product>(items, items.Length, 1, 1));
        }

        public Task<IReadOnlyCollection<string>> ListarCategoriasAtivasAsync(CancellationToken cancellationToken)
        {
            var categorias = _products.Where(product => product.Active).Select(product => product.Category).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return Task.FromResult<IReadOnlyCollection<string>>(categorias);
        }

        public Task<Product> AdicionarAsync(Product produto, CancellationToken cancellationToken)
        {
            var persisted = Product.Reidratar(
                _nextId++,
                produto.Title,
                produto.Price,
                produto.Description,
                produto.Category,
                produto.Image,
                produto.Rating,
                produto.Active);

            _products.Add(persisted);
            return Task.FromResult(persisted);
        }

        public Task AtualizarAsync(Product produto, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _entries = [];

        public byte[]? Get(string key)
        {
            return _entries.TryGetValue(key, out var value) ? value : null;
        }

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            return Task.FromResult(Get(key));
        }

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _entries.Remove(key);
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _entries[key] = value;
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }
}