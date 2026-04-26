using Ambev.DeveloperEvaluation.Users.Application.Common;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Ambev.DeveloperEvaluation.Users.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Users.Application.Repositories;

public interface IUserRepository
{
    Task<User?> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<User>> ListarAtivosAsync(CancellationToken cancellationToken);
    Task<PagedResult<User>> ListarAsync(UserListFilter filtro, CancellationToken cancellationToken);
    Task<User> AdicionarAsync(User usuario, CancellationToken cancellationToken);
    Task AtualizarAsync(User usuario, CancellationToken cancellationToken);
    Task RemoverAsync(long usuarioId, CancellationToken cancellationToken);
}
