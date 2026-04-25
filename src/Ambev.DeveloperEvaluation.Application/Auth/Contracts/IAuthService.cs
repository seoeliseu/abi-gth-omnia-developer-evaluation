using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Auth.Contracts;

public interface IAuthService
{
    Task<Result<AuthenticatedUser>> AutenticarAsync(string nomeUsuario, string senha, CancellationToken cancellationToken);
}