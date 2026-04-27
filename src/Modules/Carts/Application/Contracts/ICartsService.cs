using Ambev.DeveloperEvaluation.Carts.Application.Common;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Carts.Application.Contracts;

public interface ICartsService
{
    Task<Result<CartReference>> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<CartReference>>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken);
    Task<Result<PagedResult<CartReference>>> ListarAsync(CartListFilter filtro, CancellationToken cancellationToken);
    Task<Result<CartReference>> CriarAsync(UpsertCartRequest requisicao, CancellationToken cancellationToken);
    Task<Result<CartReference>> AtualizarAsync(long carrinhoId, UpsertCartRequest requisicao, CancellationToken cancellationToken);
    Task<Result<CartReference>> RemoverAsync(long carrinhoId, CancellationToken cancellationToken);
}
