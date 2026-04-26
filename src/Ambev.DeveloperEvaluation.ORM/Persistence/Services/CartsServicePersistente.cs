using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class CartsServicePersistente : ICartsService
{
    private readonly DeveloperEvaluationDbContext _context;

    public CartsServicePersistente(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CartReference>> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var carrinho = await _context.Carts.AsNoTracking().Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinhoId, cancellationToken);
        return carrinho is null
            ? Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")])
            : Result<CartReference>.Success(Mapear(carrinho));
    }

    public async Task<Result<IReadOnlyCollection<CartReference>>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var carrinhos = await _context.Carts.AsNoTracking().Include(item => item.Products).Where(item => item.UserId == usuarioId).OrderBy(item => item.Id).ToArrayAsync(cancellationToken);
        return Result<IReadOnlyCollection<CartReference>>.Success(carrinhos.Select(Mapear).ToArray());
    }

    public async Task<Result<PagedResult<CartReference>>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken)
    {
        var consulta = _context.Carts.AsNoTracking().Include(item => item.Products).AsQueryable();
        if (filtro.UserId.HasValue) consulta = consulta.Where(item => item.UserId == filtro.UserId.Value);
        if (filtro.MinDate.HasValue) consulta = consulta.Where(item => item.Date >= filtro.MinDate.Value);
        if (filtro.MaxDate.HasValue) consulta = consulta.Where(item => item.Date <= filtro.MaxDate.Value);

        consulta = (filtro.Order ?? string.Empty).ToLowerInvariant() switch
        {
            var valor when valor.Contains("userid desc") => consulta.OrderByDescending(item => item.UserId).ThenByDescending(item => item.Id),
            var valor when valor.Contains("userid") => consulta.OrderBy(item => item.UserId).ThenBy(item => item.Id),
            var valor when valor.Contains("date desc") => consulta.OrderByDescending(item => item.Date),
            var valor when valor.Contains("date") => consulta.OrderBy(item => item.Date),
            var valor when valor.Contains("id desc") => consulta.OrderByDescending(item => item.Id),
            _ => consulta.OrderBy(item => item.Id)
        };

        var page = filtro.Page <= 0 ? 1 : filtro.Page;
        var size = filtro.Size <= 0 ? 10 : Math.Min(filtro.Size, 100);
        var totalItems = await consulta.CountAsync(cancellationToken);
        var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)size);
        var data = await consulta.Skip((page - 1) * size).Take(size).ToArrayAsync(cancellationToken);
        return Result<PagedResult<CartReference>>.Success(new PagedResult<CartReference>(data.Select(Mapear).ToArray(), totalItems, page, totalPages));
    }

    public async Task<Result<CartReference>> CriarAsync(UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        var entidade = new CartEntity
        {
            UserId = requisicao.UserId,
            Date = requisicao.Date,
            Products = requisicao.Products.Select(item => new CartItemEntity { ProductId = item.ProductId, Quantity = item.Quantidade }).ToList()
        };

        _context.Carts.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CartReference>.Success(Mapear(entidade));
    }

    public async Task<Result<CartReference>> AtualizarAsync(long carrinhoId, UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var carrinho = await _context.Carts.Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinhoId, cancellationToken);
        if (carrinho is null) return Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]);
        var validacao = Validar(requisicao);
        if (validacao is not null) return validacao;

        carrinho.UserId = requisicao.UserId;
        carrinho.Date = requisicao.Date;
        _context.CartItems.RemoveRange(carrinho.Products);
        carrinho.Products = requisicao.Products.Select(item => new CartItemEntity { ProductId = item.ProductId, Quantity = item.Quantidade }).ToList();
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CartReference>.Success(Mapear(carrinho));
    }

    public async Task<Result<CartReference>> RemoverAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var carrinho = await _context.Carts.Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinhoId, cancellationToken);
        if (carrinho is null) return Result<CartReference>.NotFound([new ResultError("carrinho_nao_encontrado", "O carrinho informado não foi encontrado.")]);
        _context.Carts.Remove(carrinho);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<CartReference>.Success(Mapear(carrinho));
    }

    private static CartReference Mapear(CartEntity entidade) => new(entidade.Id, entidade.UserId, entidade.Date, entidade.Products.Select(item => new CartItemReference(item.ProductId, item.Quantity)).ToArray());
    private static Result<CartReference>? Validar(UpsertCartRequest requisicao)
    {
        var erros = new List<ResultError>();
        if (requisicao.UserId <= 0) erros.Add(new ResultError("usuario_invalido", "O usuário do carrinho deve ser válido."));
        if (requisicao.Products.Count == 0) erros.Add(new ResultError("produtos_obrigatorios", "O carrinho deve possuir ao menos um produto."));
        if (requisicao.Products.Any(item => item.ProductId <= 0 || item.Quantidade <= 0)) erros.Add(new ResultError("produto_invalido", "Todos os produtos do carrinho devem possuir identificador e quantidade válidos."));
        return erros.Count > 0 ? Result<CartReference>.Validation(erros) : null;
    }
}