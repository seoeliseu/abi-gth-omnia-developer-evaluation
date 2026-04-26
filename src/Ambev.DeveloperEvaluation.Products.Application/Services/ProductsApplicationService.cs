using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Products.Application.Common;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Repositories;
using Ambev.DeveloperEvaluation.Products.Domain.Entities;
using Ambev.DeveloperEvaluation.Products.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Products.Application.Services;

public sealed class ProductsApplicationService : IProductsService
{
    private readonly IProductRepository _productRepository;

    public ProductsApplicationService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Result<ProductReference>> ObterPorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _productRepository.ObterAtivoPorIdAsync(produtoId, cancellationToken);
        return produto is null
            ? Result<ProductReference>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")])
            : Result<ProductReference>.Success(MapearReferencia(produto));
    }

    public async Task<Result<IReadOnlyCollection<ProductReference>>> ListarPorIdsAsync(IReadOnlyCollection<long> produtosIds, CancellationToken cancellationToken)
    {
        var produtos = await _productRepository.ListarAtivosPorIdsAsync(produtosIds, cancellationToken);
        return Result<IReadOnlyCollection<ProductReference>>.Success(produtos.Select(MapearReferencia).ToArray());
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarAsync(ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var pagina = await _productRepository.ListarAtivosAsync(filtro, cancellationToken);
        return Result<PagedResult<ProductDetail>>.Success(MapearPagina(pagina));
    }

    public async Task<Result<ProductDetail>> CriarAsync(UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        var produto = Product.Criar(
            requisicao.Title,
            requisicao.Price,
            requisicao.Description,
            requisicao.Category,
            requisicao.Image,
            new ProductRating(requisicao.Rating.Rate, requisicao.Rating.Count));

        var persistido = await _productRepository.AdicionarAsync(produto, cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(persistido));
    }

    public async Task<Result<ProductDetail>> AtualizarAsync(long produtoId, UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var produto = await _productRepository.ObterAtivoPorIdAsync(produtoId, cancellationToken);
        if (produto is null)
        {
            return Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]);
        }

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return validacao;
        }

        produto.AtualizarDetalhes(
            requisicao.Title,
            requisicao.Price,
            requisicao.Description,
            requisicao.Category,
            requisicao.Image,
            new ProductRating(requisicao.Rating.Rate, requisicao.Rating.Count));

        await _productRepository.AtualizarAsync(produto, cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(produto));
    }

    public async Task<Result<ProductDetail>> ObterDetalhePorIdAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _productRepository.ObterAtivoPorIdAsync(produtoId, cancellationToken);
        return produto is null
            ? Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")])
            : Result<ProductDetail>.Success(MapearDetalhe(produto));
    }

    public async Task<Result<ProductDetail>> RemoverAsync(long produtoId, CancellationToken cancellationToken)
    {
        var produto = await _productRepository.ObterAtivoPorIdAsync(produtoId, cancellationToken);
        if (produto is null)
        {
            return Result<ProductDetail>.NotFound([new ResultError("produto_nao_encontrado", "O produto informado não foi encontrado.")]);
        }

        produto.Desativar();
        await _productRepository.AtualizarAsync(produto, cancellationToken);
        return Result<ProductDetail>.Success(MapearDetalhe(produto));
    }

    public async Task<Result<IReadOnlyCollection<string>>> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        var categorias = await _productRepository.ListarCategoriasAtivasAsync(cancellationToken);
        return Result<IReadOnlyCollection<string>>.Success(categorias);
    }

    public async Task<Result<PagedResult<ProductDetail>>> ListarPorCategoriaAsync(string categoria, ProductListFilter filtro, CancellationToken cancellationToken)
    {
        var pagina = await _productRepository.ListarAtivosPorCategoriaAsync(categoria, filtro, cancellationToken);
        return Result<PagedResult<ProductDetail>>.Success(MapearPagina(pagina));
    }

    private static ProductReference MapearReferencia(Product produto) => new(produto.Id, produto.Title, produto.Price, produto.Category, produto.Active);

    private static ProductDetail MapearDetalhe(Product produto) => new(
        produto.Id,
        produto.Title,
        produto.Price,
        produto.Description,
        produto.Category,
        produto.Image,
        new ProductRatingData(produto.Rating.Rate, produto.Rating.Count),
        produto.Active);

    private static PagedResult<ProductDetail> MapearPagina(PagedResult<Product> pagina)
    {
        return new PagedResult<ProductDetail>(pagina.Data.Select(MapearDetalhe).ToArray(), pagina.TotalItems, pagina.CurrentPage, pagina.TotalPages);
    }

    private static Result<ProductDetail>? Validar(UpsertProductRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (string.IsNullOrWhiteSpace(requisicao.Title)) erros.Add(new ResultError("title_obrigatorio", "O título do produto é obrigatório."));
        if (requisicao.Price <= 0) erros.Add(new ResultError("price_invalido", "O preço do produto deve ser maior que zero."));
        if (string.IsNullOrWhiteSpace(requisicao.Category)) erros.Add(new ResultError("category_obrigatoria", "A categoria do produto é obrigatória."));
        return erros.Count > 0 ? Result<ProductDetail>.Validation(erros) : null;
    }
}
