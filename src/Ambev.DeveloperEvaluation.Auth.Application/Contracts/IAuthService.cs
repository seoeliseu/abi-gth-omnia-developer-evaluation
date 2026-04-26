using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Auth.Application.Contracts;

public interface IAuthService
{
    Task<Result<AuthenticatedUser>> AutenticarAsync(string nomeUsuario, string senha, CancellationToken cancellationToken);
    Task<Result<AuthenticatedUser>> AutenticarAsync(LoginRequest requisicao, CancellationToken cancellationToken);
}
