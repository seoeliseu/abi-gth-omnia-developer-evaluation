using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Carts.Application.Repositories;
using Ambev.DeveloperEvaluation.Carts.Domain.Entities;
using Ambev.DeveloperEvaluation.Carts.Domain.ValueObjects;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.Carts.Infrastructure.Persistence.Repositories;

public sealed class CartRepositoryEf : ICartRepository
{
    private readonly DeveloperEvaluationDbContext _context;

    public CartRepositoryEf(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<Cart?> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var carrinho = await _context.Carts.AsNoTracking().Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinhoId, cancellationToken);
        return carrinho is null ? null : Mapear(carrinho);
    }

    public async Task<IReadOnlyCollection<Cart>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken)
    {
        var carrinhos = await _context.Carts.AsNoTracking().Include(item => item.Products).Where(item => item.UserId == usuarioId).OrderBy(item => item.Id).ToArrayAsync(cancellationToken);
        return carrinhos.Select(Mapear).ToArray();
    }

    public async Task<PagedResult<Cart>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken)
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
        return new PagedResult<Cart>(data.Select(Mapear).ToArray(), totalItems, page, totalPages);
    }

    public async Task<Cart> AdicionarAsync(Cart carrinho, CancellationToken cancellationToken)
    {
        var entidade = new CartEntity
        {
            UserId = carrinho.UsuarioId,
            Date = carrinho.Data,
            Products = carrinho.Produtos.Select(item => new CartItemEntity { ProductId = item.ProductId, Quantity = item.Quantidade }).ToList()
        };

        _context.Carts.Add(entidade);
        await _context.SaveChangesAsync(cancellationToken);
        return Mapear(entidade);
    }

    public async Task AtualizarAsync(Cart carrinho, CancellationToken cancellationToken)
    {
        var entidade = await _context.Carts.Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinho.Id, cancellationToken)
            ?? throw new InvalidOperationException("O carrinho informado não foi encontrado para persistência.");

        entidade.UserId = carrinho.UsuarioId;
        entidade.Date = carrinho.Data;
        _context.CartItems.RemoveRange(entidade.Products);
        entidade.Products = carrinho.Produtos.Select(item => new CartItemEntity { ProductId = item.ProductId, Quantity = item.Quantidade }).ToList();
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(long carrinhoId, CancellationToken cancellationToken)
    {
        var entidade = await _context.Carts.Include(item => item.Products).SingleOrDefaultAsync(item => item.Id == carrinhoId, cancellationToken)
            ?? throw new InvalidOperationException("O carrinho informado não foi encontrado para persistência.");
        _context.Carts.Remove(entidade);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static Cart Mapear(CartEntity entidade)
        => Cart.Reidratar(entidade.Id, entidade.UserId, entidade.Date, entidade.Products.Select(item => new CartItem(item.ProductId, item.Quantity)));
}
