using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Carts.Application.Repositories;
using Ambev.DeveloperEvaluation.Carts.Domain.Entities;
using Ambev.DeveloperEvaluation.Carts.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Carts.Application.Services;

public sealed class CartsApplicationService : ICartsService
{
    private readonly ICartRepository _cartRepository;

    public CartsApplicationService(ICartRepository cartRepository)
    {
        _cartRepository = cartRepository;
    }

    public async Task<Result<CartReference>> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var carrinho = await _cartRepository.ObterPorIdAsync(carrinhoId, cancellationToken);
        return carrinho is null
            ? Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")])
            : Result<CartReference>.Success(Mapear(carrinho));
    }

    public async Task<Result<IReadOnlyCollection<CartReference>>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var carrinhos = await _cartRepository.ListarPorUsuarioAsync(usuarioId, cancellationToken);
        return Result<IReadOnlyCollection<CartReference>>.Success(carrinhos.Select(Mapear).ToArray());
    }

    public async Task<Result<PagedResult<CartReference>>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken)
    {
        var pagina = await _cartRepository.ListarAsync(filtro, cancellationToken);
        return Result<PagedResult<CartReference>>.Success(new PagedResult<CartReference>(pagina.Data.Select(Mapear).ToArray(), pagina.TotalItems, pagina.CurrentPage, pagina.TotalPages));
    }

    public async Task<Result<CartReference>> CriarAsync(UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        var carrinho = Cart.Criar(requisicao.UserId, requisicao.Date, requisicao.Products.Select(item => new CartItem(item.ProductId, item.Quantidade)));
        var persistido = await _cartRepository.AdicionarAsync(carrinho, cancellationToken);
        return Result<CartReference>.Success(Mapear(persistido));
    }

    public async Task<Result<CartReference>> AtualizarAsync(long carrinhoId, UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var carrinho = await _cartRepository.ObterPorIdAsync(carrinhoId, cancellationToken);
        if (carrinho is null) return Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]);
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        carrinho.Atualizar(requisicao.UserId, requisicao.Date, requisicao.Products.Select(item => new CartItem(item.ProductId, item.Quantidade)));
        await _cartRepository.AtualizarAsync(carrinho, cancellationToken);
        return Result<CartReference>.Success(Mapear(carrinho));
    }

    public async Task<Result<CartReference>> RemoverAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var carrinho = await _cartRepository.ObterPorIdAsync(carrinhoId, cancellationToken);
        if (carrinho is null) return Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]);
        await _cartRepository.RemoverAsync(carrinhoId, cancellationToken);
        return Result<CartReference>.Success(Mapear(carrinho));
    }

    private static CartReference Mapear(Cart entidade)
        => new(entidade.Id, entidade.UsuarioId, entidade.Data, entidade.Produtos.Select(item => new CartItemReference(item.ProductId, item.Quantidade)).ToArray());

    private static Result<CartReference>? Validar(UpsertCartRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (requisicao.UserId <= 0) erros.Add(new ResultError("usuario_invalido", "O usuário do carrinho deve ser válido."));
        if (requisicao.Products.Count == 0) erros.Add(new ResultError("produtos_obrigatorios", "O carrinho deve possuir ao menos um produto."));
        if (requisicao.Products.Any(item => item.ProductId <= 0 || item.Quantidade <= 0)) erros.Add(new ResultError("produto_invalido", "Todos os produtos do carrinho devem possuir identificador e quantidade válidos."));
        return erros.Count > 0 ? Result<CartReference>.Validation(erros) : null;
    }
}
