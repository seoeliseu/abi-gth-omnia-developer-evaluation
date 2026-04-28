using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Users.Application.Common;

namespace Ambev.DeveloperEvaluation.Users.Application.Contracts;

public interface IUsersService
{
    Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken);
    Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken);
    Task<Result<PagedResult<UserDetail>>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken);
    Task<Result<UserDetail>> ObterDetalhePorIdAsync(long usuarioId, CancellationToken cancellationToken);
    Task<Result<UserDetail>> CriarAsync(UpsertUserRequest requisicao, CancellationToken cancellationToken);
    Task<Result<UserDetail>> AtualizarAsync(long usuarioId, UpsertUserRequest requisicao, CancellationToken cancellationToken);
    Task<Result<UserDetail>> RemoverAsync(long usuarioId, CancellationToken cancellationToken);
}
