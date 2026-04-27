using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class CartsServiceEmMemoria : ICartsService
{
    private readonly ConcurrentDictionary<long, CartReference> _carts = new();
    private long _currentId = 2;

    public CartsServiceEmMemoria()
    {
        _carts[1] = new CartReference(1, 1, new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero), [new CartItemReference(10, 2)]);
        _carts[2] = new CartReference(2, 2, new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero), [new CartItemReference(20, 1), new CartItemReference(21, 4)]);
    }

    public Task<Result<CartReference>> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_carts.TryGetValue(carrinhoId, out var carrinho)
            ? Result<CartReference>.Success(carrinho)
            : Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]));
    }

    public Task<Result<IReadOnlyCollection<CartReference>>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyCollection<CartReference> carrinhos = _carts.Values.Where(cart => cart.UsuarioId == usuarioId).OrderBy(cart => cart.Id).ToArray();
        return Task.FromResult(Result<IReadOnlyCollection<CartReference>>.Success(carrinhos));
    }

    public Task<Result<PagedResult<CartReference>>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var pagina = filtro.Page <= 0 ? 1 : filtro.Page;
        var tamanho = filtro.Size <= 0 ? 10 : Math.Min(filtro.Size, 100);
        var consulta = _carts.Values.AsEnumerable();

        if (filtro.UserId.HasValue)
        {
            consulta = consulta.Where(cart => cart.UsuarioId == filtro.UserId.Value);
        }

        if (filtro.MinDate.HasValue)
        {
            consulta = consulta.Where(cart => cart.Data >= filtro.MinDate.Value);
        }

        if (filtro.MaxDate.HasValue)
        {
            consulta = consulta.Where(cart => cart.Data <= filtro.MaxDate.Value);
        }

        consulta = AplicarOrdenacao(consulta, filtro.Order);
        var totalItems = consulta.Count();
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)tamanho);
        var dados = consulta.Skip((pagina - 1) * tamanho).Take(tamanho).ToArray();

        return Task.FromResult(Result<PagedResult<CartReference>>.Success(new PagedResult<CartReference>(dados, totalItems, pagina, totalPages)));
    }

    public Task<Result<CartReference>> CriarAsync(UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var id = Interlocked.Increment(ref _currentId);
        var carrinho = new CartReference(id, requisicao.UserId, requisicao.Date, requisicao.Products.ToArray());
        _carts[id] = carrinho;
        return Task.FromResult(Result<CartReference>.Success(carrinho));
    }

    public Task<Result<CartReference>> AtualizarAsync(long carrinhoId, UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_carts.ContainsKey(carrinhoId))
        {
            return Task.FromResult(Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]));
        }

        var validacao = Validar(requisicao);
        if (validacao is not null)
        {
            return Task.FromResult(validacao);
        }

        var carrinho = new CartReference(carrinhoId, requisicao.UserId, requisicao.Date, requisicao.Products.ToArray());
        _carts[carrinhoId] = carrinho;
        return Task.FromResult(Result<CartReference>.Success(carrinho));
    }

    public Task<Result<CartReference>> RemoverAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_carts.TryRemove(carrinhoId, out var carrinho)
            ? Result<CartReference>.Success(carrinho)
            : Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]));
    }

    private static Result<CartReference>? Validar(UpsertCartRequest requisicao)
    {
        var erros = new List<ResultError>();

        if (requisicao.UserId <= 0)
        {
            erros.Add(new ResultError("usuario_invalido", "O usuário do carrinho deve ser válido."));
        }

        if (requisicao.Products.Count == 0)
        {
            erros.Add(new ResultError("produtos_obrigatorios", "O carrinho deve possuir ao menos um produto."));
        }

        if (requisicao.Products.Any(produto => produto.ProductId <= 0 || produto.Quantidade <= 0))
        {
            erros.Add(new ResultError("produto_invalido", "Todos os produtos do carrinho devem possuir identificador e quantidade válidos."));
        }

        return erros.Count > 0 ? Result<CartReference>.Validation(erros) : null;
    }

    private static IEnumerable<CartReference> AplicarOrdenacao(IEnumerable<CartReference> consulta, string? order)
    {
        var clausulas = string.IsNullOrWhiteSpace(order)
            ? ["id asc"]
            : order.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        IOrderedEnumerable<CartReference>? ordenado = null;

        foreach (var clausula in clausulas)
        {
            var partes = clausula.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var campo = partes[0].ToLowerInvariant();
            var descendente = partes.Length > 1 && string.Equals(partes[1], "desc", StringComparison.OrdinalIgnoreCase);

            ordenado = (ordenado, campo, descendente) switch
            {
                (null, "userid", true) => consulta.OrderByDescending(cart => cart.UsuarioId),
                (null, "userid", false) => consulta.OrderBy(cart => cart.UsuarioId),
                (null, "date", true) => consulta.OrderByDescending(cart => cart.Data),
                (null, "date", false) => consulta.OrderBy(cart => cart.Data),
                (null, _, true) => consulta.OrderByDescending(cart => cart.Id),
                (null, _, false) => consulta.OrderBy(cart => cart.Id),
                (_, "userid", true) => ordenado.ThenByDescending(cart => cart.UsuarioId),
                (_, "userid", false) => ordenado.ThenBy(cart => cart.UsuarioId),
                (_, "date", true) => ordenado.ThenByDescending(cart => cart.Data),
                (_, "date", false) => ordenado.ThenBy(cart => cart.Data),
                (_, _, true) => ordenado.ThenByDescending(cart => cart.Id),
                _ => ordenado.ThenBy(cart => cart.Id)
            };
        }

        return ordenado ?? consulta.OrderBy(cart => cart.Id);
    }
}