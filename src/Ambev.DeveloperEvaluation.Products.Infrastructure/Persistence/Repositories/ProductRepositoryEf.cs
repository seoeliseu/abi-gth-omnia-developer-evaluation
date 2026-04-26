using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Products.Application.Common;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Repositories;
using Ambev.DeveloperEvaluation.Products.Domain.Entities;
using Ambev.DeveloperEvaluation.Products.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.Products.Infrastructure.Persistence.Repositories;

public sealed class ProductRepositoryEf : IProductRepository
{
    private readonly DeveloperEvaluationDbContext _context;

    public ProductRepositoryEf(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> ObterAtivoPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var entidade = await _context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == produtoId && item.Active, cancellationToken);

        return entidade is null ? null : MapearDominio(entidade);
    }

    public async Task<IReadOnlyCollection<Product>> ListarAtivosPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        var produtos = await _context.Products
            .AsNoTracking()
            .Where(item => produtosIds.Contains(item.Id) && item.Active)
            .Select(item => MapearDominio(item))
            .ToArrayAsync(cancellationToken);

        return produtos;
    }

    public Task<PagedResult<Product>> ListarAtivosAsync(ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = AplicarFiltros(_context.Products.AsNoTracking().Where(item => item.Active), filtro);
        return PaginarAsync(consulta, filtro.Page, filtro.Size, cancellationToken);
    }

    public Task<PagedResult<Product>> ListarAtivosPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = AplicarFiltros(_context.Products.AsNoTracking().Where(item => item.Active && item.Category == categoria), filtro with { Category = categoria });
        return PaginarAsync(consulta, filtro.Page, filtro.Size, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> ListarCategoriasAtivasAsync(CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(item => item.Active)
            .Select(item => item.Category)
            .Distinct()
            .OrderBy(item => item)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<Product> AdicionarAsync(Product produto, CancellationToken cancellationToken)
    {
        var entidade = new ProductEntity
        {
            Title = produto.Title,
            Price = produto.Price,
            Description = produto.Description,
            Category = produto.Category,
            Image = produto.Image,
            RatingRate = produto.Rating.Rate,
            RatingCount = produto.Rating.Count,
            Active = produto.Active
        };

        _context.Products.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return MapearDominio(entidade);
    }

    public async Task AtualizarAsync(Product produto, CancellationToken cancellationToken)
    {
        var entidade = await _context.Products.SingleOrDefaultAsync(item => item.Id == produto.Id, cancellationToken)
            ?? throw new InvalidOperationException("O produto informado não foi encontrado para persistência.");

        entidade.Title = produto.Title;
        entidade.Price = produto.Price;
        entidade.Description = produto.Description;
        entidade.Category = produto.Category;
        entidade.Image = produto.Image;
        entidade.RatingRate = produto.Rating.Rate;
        entidade.RatingCount = produto.Rating.Count;
        entidade.Active = produto.Active;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<ProductEntity> AplicarFiltros(IQueryable<ProductEntity> consulta, ProductListFilter filtro)
    {
        if (!string.IsNullOrWhiteSpace(filtro.Title))
        {
            var titulo = filtro.Title.Replace("*", string.Empty, StringComparison.Ordinal);
            consulta = consulta.Where(item => item.Title.Contains(titulo));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Category))
        {
            consulta = consulta.Where(item => item.Category == filtro.Category);
        }

        if (filtro.MinPrice.HasValue)
        {
            consulta = consulta.Where(item => item.Price >= filtro.MinPrice.Value);
        }

        if (filtro.MaxPrice.HasValue)
        {
            consulta = consulta.Where(item => item.Price <= filtro.MaxPrice.Value);
        }

        return (filtro.Order ?? string.Empty).ToLowerInvariant() switch
        {
            var valor when valor.Contains("price desc") => consulta.OrderByDescending(item => item.Price).ThenBy(item => item.Title),
            var valor when valor.Contains("price") => consulta.OrderBy(item => item.Price).ThenBy(item => item.Title),
            var valor when valor.Contains("title desc") => consulta.OrderByDescending(item => item.Title),
            _ => consulta.OrderBy(item => item.Title)
        };
    }

    private static async Task<PagedResult<Product>> PaginarAsync(IQueryable<ProductEntity> consulta, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var page = pagina <= 0 ? 1 : pagina;
        var size = tamanho <= 0 ? 10 : Math.Min(tamanho, 100);
        var totalItems = await consulta.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)size);
        var data = await consulta.Skip((page - 1) * size).Take(size).Select(item => MapearDominio(item)).ToArrayAsync(cancellationToken);
        return new PagedResult<Product>(data, totalItems, page, totalPages);
    }

    private static Product MapearDominio(ProductEntity entidade)
    {
        return Product.Reidratar(
            entidade.Id,
            entidade.Title,
            entidade.Price,
            entidade.Description,
            entidade.Category,
            entidade.Image,
            new ProductRating(entidade.RatingRate, entidade.RatingCount),
            entidade.Active);
    }
}
