using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Products.Application.Common;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Ambev.DeveloperEvaluation.Products.Infrastructure.Caching;

public sealed class CachedProductsService : IProductsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private static readonly DistributedCacheEntryOptions DetailCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
    private static readonly DistributedCacheEntryOptions ListCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
    };
    private static readonly DistributedCacheEntryOptions VersionCacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private readonly ProductsApplicationService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedProductsService> _logger;

    public CachedProductsService(
        ProductsApplicationService inner,
        IDistributedCache cache,
        ILogger<CachedProductsService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:reference:{produtoId}";
        var cached = await TryGetAsync<ProductReference>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<ProductReference>.Success(cached);
        }

        var resultado = await _inner.ObterPorIdAsync(produtoId, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, DetailCacheOptions, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        var normalizedIds = produtosIds.OrderBy(id => id).ToArray();
        var version = await GetCatalogVersionAsync(cancellationToken);
        var cacheKey = $"products:references:{version}:{ComputeHash(string.Join(',', normalizedIds))}";
        var cached = await TryGetAsync<IReadOnlyCollection<ProductReference>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<IReadOnlyCollection<ProductReference>>.Success(cached);
        }

        var resultado = await _inner.ListarPorIdsAsync(produtosIds, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, ListCacheOptions, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var version = await GetCatalogVersionAsync(cancellationToken);
        var cacheKey = $"products:list:{version}:{ComputeHash(SerializeFilter(filtro))}";
        var cached = await TryGetAsync<PagedResult<ProductDetail>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<PagedResult<ProductDetail>>.Success(cached);
        }

        var resultado = await _inner.ListarAsync(filtro, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, ListCacheOptions, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _inner.CriarAsync(requisicao, cancellationToken);
        if (resultado.IsSuccess)
        {
            await InvalidarCatalogoAsync(resultado.Value?.Id, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _inner.AtualizarAsync(produtoId, requisicao, cancellationToken);
        if (resultado.IsSuccess)
        {
            await InvalidarCatalogoAsync(produtoId, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var cacheKey = $"products:detail:{produtoId}";
        var cached = await TryGetAsync<ProductDetail>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<ProductDetail>.Success(cached);
        }

        var resultado = await _inner.ObterDetalhePorIdAsync(produtoId, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, DetailCacheOptions, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken)
    {
        var resultado = await _inner.RemoverAsync(produtoId, cancellationToken);
        if (resultado.IsSuccess)
        {
            await InvalidarCatalogoAsync(produtoId, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        var version = await GetCatalogVersionAsync(cancellationToken);
        var cacheKey = $"products:categories:{version}";
        var cached = await TryGetAsync<IReadOnlyCollection<string>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<IReadOnlyCollection<string>>.Success(cached);
        }

        var resultado = await _inner.ListarCategoriasAsync(cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, ListCacheOptions, cancellationToken);
        }

        return resultado;
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var version = await GetCatalogVersionAsync(cancellationToken);
        var cacheKey = $"products:list-by-category:{version}:{ComputeHash($"{categoria}|{SerializeFilter(filtro)}")}";
        var cached = await TryGetAsync<PagedResult<ProductDetail>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return Result<PagedResult<ProductDetail>>.Success(cached);
        }

        var resultado = await _inner.ListarPorCategoriaAsync(categoria, filtro, cancellationToken);
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            await TrySetAsync(cacheKey, resultado.Value, ListCacheOptions, cancellationToken);
        }

        return resultado;
    }

    private async Task InvalidarCatalogoAsync(long? produtoId, CancellationToken cancellationToken)
    {
        if (produtoId.HasValue)
        {
            await TryRemoveAsync($"products:reference:{produtoId.Value}", cancellationToken);
            await TryRemoveAsync($"products:detail:{produtoId.Value}", cancellationToken);
        }

        await TrySetStringAsync(
            "products:catalog:version",
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
            VersionCacheOptions,
            cancellationToken);
    }

    private async Task<string> GetCatalogVersionAsync(CancellationToken cancellationToken)
    {
        var cachedVersion = await TryGetStringAsync("products:catalog:version", cancellationToken);
        if (!string.IsNullOrWhiteSpace(cachedVersion))
        {
            return cachedVersion;
        }

        const string initialVersion = "1";
        await TrySetStringAsync("products:catalog:version", initialVersion, VersionCacheOptions, cancellationToken);
        return initialVersion;
    }

    private async Task<T?> TryGetAsync<T>(string key, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _cache.GetStringAsync(key, cancellationToken);
            return string.IsNullOrWhiteSpace(payload)
                ? default
                : JsonSerializer.Deserialize<T>(payload, SerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler cache Redis para a chave {CacheKey}.", key);
            return default;
        }
    }

    private async Task TrySetAsync<T>(string key, T value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            await _cache.SetStringAsync(key, payload, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao escrever cache Redis para a chave {CacheKey}.", key);
        }
    }

    private async Task<string?> TryGetStringAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await _cache.GetStringAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao ler cache Redis para a chave {CacheKey}.", key);
            return null;
        }
    }

    private async Task TrySetStringAsync(string key, string value, DistributedCacheEntryOptions options, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.SetStringAsync(key, value, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao escrever cache Redis para a chave {CacheKey}.", key);
        }
    }

    private async Task TryRemoveAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await _cache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao remover cache Redis para a chave {CacheKey}.", key);
        }
    }

    private static string SerializeFilter(ProductListFilter filtro)
    {
        return string.Join('|',
            filtro.Page.ToString(CultureInfo.InvariantCulture),
            filtro.Size.ToString(CultureInfo.InvariantCulture),
            filtro.Order ?? string.Empty,
            filtro.Category ?? string.Empty,
            filtro.Title ?? string.Empty,
            filtro.MinPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            filtro.MaxPrice?.ToString(CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static string ComputeHash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}