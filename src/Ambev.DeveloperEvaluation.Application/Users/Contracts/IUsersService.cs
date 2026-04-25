using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Users.Contracts;

public interface IUsersService
{
    Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken);
}