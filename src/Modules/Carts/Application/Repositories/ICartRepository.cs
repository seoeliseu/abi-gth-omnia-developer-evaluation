using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Carts.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Carts.Application.Repositories;

public interface ICartRepository
{
    Task<Cart?> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<Cart>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken);
    Task<PagedResult<Cart>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken);
    Task<Cart> AdicionarAsync(Cart carrinho, CancellationToken cancellationToken);
    Task AtualizarAsync(Cart carrinho, CancellationToken cancellationToken);
    Task RemoverAsync(long carrinhoId, CancellationToken cancellationToken);
}
