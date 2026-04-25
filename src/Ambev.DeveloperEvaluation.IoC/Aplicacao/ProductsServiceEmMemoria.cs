using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class ProductsServiceEmMemoria : IProductsService
{
    private readonly ConcurrentDictionary<long, ProductDetail> _products = new();
    private long _currentId = 3;

    public ProductsServiceEmMemoria()
    {
        _products[1] = new ProductDetail(1, "Fjallraven Backpack", 109.95m, "Mochila para uso diário", "men's clothing", "https://example.com/products/1.png", new ProductRatingData(3.9m, 120), true);
        _products[2] = new ProductDetail(2, "Premium Beer Box", 39.90m, "Caixa de cervejas premium", "beverage", "https://example.com/products/2.png", new ProductRatingData(4.8m, 45), true);
        _products[3] = new ProductDetail(3, "Potato Chips", 8.50m, "Batata chips tamanho família", "snack", "https://example.com/products/3.png", new ProductRatingData(4.2m, 210), true);
    }

    public Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_products.TryGetValue(produtoId, out var produto)
            ? Result<ProductReference>.Success(MapearReferencia(produto))
            : Result<ProductReference>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]));
    }

    public Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var produtos = produtosIds
            .Where(id => _products.ContainsKey(id))
            .Distinct()
            .Select(id => MapearReferencia(_products[id]))
            .ToArray();

        return Task.FromResult(Result<IReadOnlyCollection<ProductReference>>.Success(produtos));
    }

    public Task<Result<PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var consulta = Filtrar(_products.Values, filtro);
        var resultado = Paginar(consulta, filtro.Page, filtro.Size);
        return Task.FromResult(Result<PagedResult<ProductDetail>>.Success(resultado));
    }

    public Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var id = Interlocked.Increment(ref _currentId);
        var produto = new ProductDetail(id, requisicao.Title, requisicao.Price, requisicao.Description, requisicao.Category, requisicao.Image, requisicao.Rating, true);
        _products[id] = produto;
        return Task.FromResult(Result<ProductDetail>.Success(produto));
    }

    public Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_products.ContainsKey(produtoId))
        {
            return Task.FromResult(Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]));
        }

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var produto = new ProductDetail(produtoId, requisicao.Title, requisicao.Price, requisicao.Description, requisicao.Category, requisicao.Image, requisicao.Rating, true);
        _products[produtoId] = produto;
        return Task.FromResult(Result<ProductDetail>.Success(produto));
    }

    public Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_products.TryGetValue(produtoId, out var produto)
            ? Result<ProductDetail>.Success(produto)
            : Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]));
    }

    public Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_products.TryRemove(produtoId, out var produto)
            ? Result<ProductDetail>.Success(produto)
            : Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]));
    }

    public Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<string> categorias = _products.Values.Select(produto => produto.Category).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(categoria => categoria).ToArray();
        return Task.FromResult(Result<IReadOnlyCollection<string>>.Success(categorias));
    }

    public Task<Result<PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var consulta = Filtrar(_products.Values, filtro with { Category = categoria });
        var resultado = Paginar(consulta, filtro.Page, filtro.Size);
        return Task.FromResult(Result<PagedResult<ProductDetail>>.Success(resultado));
    }

    private static ProductReference MapearReferencia(ProductDetail produto)
    {
        return new ProductReference(produto.Id, produto.Title, produto.Price, produto.Category, produto.Active);
    }

    private static IEnumerable<ProductDetail> Filtrar(IEnumerable<ProductDetail> produtos, ProductListFilter filtro)
    {
        var consulta = produtos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(filtro.Category))
        {
            consulta = consulta.Where(produto => string.Equals(produto.Category, filtro.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Title))
        {
            var valor = filtro.Title.Replace("*", string.Empty, StringComparison.Ordinal);
            consulta = consulta.Where(produto => produto.Title.Contains(valor, StringComparison.OrdinalIgnoreCase));
        }

        if (filtro.MinPrice.HasValue)
        {
            consulta = consulta.Where(produto => produto.Price >= filtro.MinPrice.Value);
        }

        if (filtro.MaxPrice.HasValue)
        {
            consulta = consulta.Where(produto => produto.Price <= filtro.MaxPrice.Value);
        }

        return AplicarOrdenacao(consulta, filtro.Order);
    }

    private static PagedResult<ProductDetail> Paginar(IEnumerable<ProductDetail> consulta, int page, int size)
    {
        var pagina = page <= 0 ? 1 : page;
        var tamanho = size <= 0 ? 10 : Math.Min(size, 100);
        var totalItems = consulta.Count();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanho);
        var data = consulta.Skip((pagina - 1) * tamanho).Take(tamanho).ToArray();
        return new PagedResult<ProductDetail>(data, totalItems, pagina, totalPages);
    }

    private static IEnumerable<ProductDetail> AplicarOrdenacao(IEnumerable<ProductDetail> consulta, string? order)
    {
        var clausulas = string.IsNullOrWhiteSpace(order)
            ? ["title asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<ProductDetail>? ordenado = null;
        foreach (var clausula in clausulas)
        {
            var partes = clausula.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var campo = partes[0].ToLowerInvariant();
            var descendente = partes.Length > 1 && string.Equals(partes[1], "desc", StringComparison.OrdinalIgnoreCase);

            ordenado = (ordenado, campo, descendente) switch
            {
                (null, "price", true) => consulta.OrderByDescending(produto => produto.Price),
                (null, "price", false) => consulta.OrderBy(produto => produto.Price),
                (null, _, true) => consulta.OrderByDescending(produto => produto.Title),
                (null, _, false) => consulta.OrderBy(produto => produto.Title),
                (_, "price", true) => ordenado.ThenByDescending(produto => produto.Price),
                (_, "price", false) => ordenado.ThenBy(produto => produto.Price),
                (_, _, true) => ordenado.ThenByDescending(produto => produto.Title),
                _ => ordenado.ThenBy(produto => produto.Title)
            };
        }

        return ordenado ?? consulta.OrderBy(produto => produto.Title);
    }

    private static Result<ProductDetail>? Validar(UpsertProductRequest requisicao)
    {
        var erros = new List<ResultError>();

        if (string.IsNullOrWhiteSpace(requisicao.Title))
        {
            erros.Add(new ResultError("title_obrigatorio", "O título do produto é obrigatório."));
        }

        if (requisicao.Price <= 0)
        {
            erros.Add(new ResultError("price_invalido", "O preço do produto deve ser maior que zero."));
        }

        if (string.IsNullOrWhiteSpace(requisicao.Category))
        {
            erros.Add(new ResultError("category_obrigatoria", "A categoria do produto é obrigatória."));
        }

        return erros.Count > 0 ? Result<ProductDetail>.Validation(erros) : null;
    }
}