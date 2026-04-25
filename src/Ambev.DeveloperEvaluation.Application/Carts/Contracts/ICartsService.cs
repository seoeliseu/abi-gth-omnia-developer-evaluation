using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Carts.Contracts;

public interface ICartsService
{
    Task<Result<CartReference>> ObterPorIdAsync(long carrinhoId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<CartReference>>> ListarPorUsuarioAsync(long usuarioId, CancellationToken cancellationToken);
}