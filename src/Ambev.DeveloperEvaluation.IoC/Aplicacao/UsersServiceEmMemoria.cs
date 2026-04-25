using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class UsersServiceEmMemoria : IUsersService
{
    public Task<Result<UserReference>> ObterPorIdAsync(long usuarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (usuarioId <= 0)
        {
            return Task.FromResult(Result<UserReference>.NotFound([new ResultError("usuario_nao_encontrado", "O usuário informado não foi encontrado.")]));
        }

        var usuario = new UserReference(usuarioId, $"usuario{usuarioId}", $"usuario{usuarioId}@example.com", "Active", "Customer", true);
        return Task.FromResult(Result<UserReference>.Success(usuario));
    }

    public Task<Result<IReadOnlyCollection<UserReference>>> ListarAtivosAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyCollection<UserReference> usuarios =
        [
            new UserReference(1, "usuario1", "usuario1@example.com", "Active", "Customer", true),
            new UserReference(2, "usuario2", "usuario2@example.com", "Active", "Manager", true)
        ];

        return Task.FromResult(Result<IReadOnlyCollection<UserReference>>.Success(usuarios));
    }
}