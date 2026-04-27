using Ambev.DeveloperEvaluation.Auth.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Auth.Application.Repositories;

public interface IAuthUserRepository
{
    Task<AuthUser?> ObterPorNomeUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken);
}
