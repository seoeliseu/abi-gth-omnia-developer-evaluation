using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class ProductsServicePersistente : IProductsService
{
    private readonly DeveloperEvaluationDbContext _context;

    public ProductsServicePersistente(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _context.Products.SingleOrDefaultAsync(item => item.Id == produtoId && item.Active, cancellationToken);
        return produto is null
            ? Result<ProductReference>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")])
            : Result<ProductReference>.Success(MapearReferencia(produto));
    }

    public async Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        var produtos = await _context.Products
            .Where(item => produtosIds.Contains(item.Id) && item.Active)
            .Select(item => MapearReferencia(item))
            .ToArrayAsync(cancellationToken);

        return Result<IReadOnlyCollection<ProductReference>>.Success(produtos);
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = AplicarFiltros(_context.Products.AsNoTracking().Where(item => item.Active), filtro);
        return Result<PagedResult<ProductDetail>>.Success(await PaginarAsync(consulta, filtro.Page, filtro.Size, cancellationToken));
    }

    public async Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        var entidade = new ProductEntity
        {
            Title = requisicao.Title,
            Price = requisicao.Price,
            Description = requisicao.Description,
            Category = requisicao.Category,
            Image = requisicao.Image,
            RatingRate = requisicao.Rating.Rate,
            RatingCount = requisicao.Rating.Count,
            Active = true
        };

        _context.Products.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(entidade));
    }

    public async Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var entidade = await _context.Products.SingleOrDefaultAsync(item => item.Id == produtoId && item.Active, cancellationToken);
        if (entidade is null)
        {
            return Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]);
        }

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        entidade.Title = requisicao.Title;
        entidade.Price = requisicao.Price;
        entidade.Description = requisicao.Description;
        entidade.Category = requisicao.Category;
        entidade.Image = requisicao.Image;
        entidade.RatingRate = requisicao.Rating.Rate;
        entidade.RatingCount = requisicao.Rating.Count;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(entidade));
    }

    public async Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _context.Products.AsNoTracking().SingleOrDefaultAsync(item => item.Id == produtoId && item.Active, cancellationToken);
        return produto is null
            ? Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")])
            : Result<ProductDetail>.Success(MapearDetalhe(produto));
    }

    public async Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _context.Products.SingleOrDefaultAsync(item => item.Id == produtoId && item.Active, cancellationToken);
        if (produto is null)
        {
            return Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]);
        }

        produto.Active = false;
        await _context.SaveChangesAsync(cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(produto));
    }

    public async Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        var categorias = await _context.Products.AsNoTracking().Where(item => item.Active).Select(item => item.Category).Distinct().OrderBy(item => item).ToArrayAsync(cancellationToken);
        return Result<IReadOnlyCollection<string>>.Success(categorias);
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = AplicarFiltros(_context.Products.AsNoTracking().Where(item => item.Active && item.Category == categoria), filtro with { Category = categoria });
        return Result<PagedResult<ProductDetail>>.Success(await PaginarAsync(consulta, filtro.Page, filtro.Size, cancellationToken));
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

    private static async Task<PagedResult<ProductDetail>> PaginarAsync(IQueryable<ProductEntity> consulta, int pagina, int tamanho, CancellationToken cancellationToken)
    {
        var page = pagina <= 0 ? 1 : pagina;
        var size = tamanho <= 0 ? 10 : Math.Min(tamanho, 100);
        var totalItems = await consulta.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)size);
        var data = await consulta.Skip((page - 1) * size).Take(size).Select(item => MapearDetalhe(item)).ToArrayAsync(cancellationToken);
        return new PagedResult<ProductDetail>(data, totalItems, page, totalPages);
    }

    private static ProductReference MapearReferencia(ProductEntity entidade) => new(entidade.Id, entidade.Title, entidade.Price, entidade.Category, entidade.Active);
    private static ProductDetail MapearDetalhe(ProductEntity entidade) => new(entidade.Id, entidade.Title, entidade.Price, entidade.Description, entidade.Category, entidade.Image, new ProductRatingData(entidade.RatingRate, entidade.RatingCount), entidade.Active);
    private static Result<ProductDetail>? Validar(UpsertProductRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (string.IsNullOrWhiteSpace(requisicao.Title)) erros.Add(new ResultError("title_obrigatorio", "O título do produto é obrigatório."));
        if (requisicao.Price <= 0) erros.Add(new ResultError("price_invalido", "O preço do produto deve ser maior que zero."));
        if (string.IsNullOrWhiteSpace(requisicao.Category)) erros.Add(new ResultError("category_obrigatoria", "A categoria do produto é obrigatória."));
        return erros.Count > 0 ? Result<ProductDetail>.Validation(erros) : null;
    }
}