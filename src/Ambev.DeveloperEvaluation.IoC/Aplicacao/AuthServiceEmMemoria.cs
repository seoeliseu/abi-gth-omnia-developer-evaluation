using Ambev.DeveloperEvaluation.Application.Auth.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class AuthServiceEmMemoria : IAuthService
{
    public Task<Result<AuthenticatedUser>> AutenticarAsync(string nomeUsuario, string senha, CancellationToken cancellationToken)
    {
        return AutenticarAsync(new LoginRequest(nomeUsuario, senha), cancellationToken);
    }

    public Task<Result<AuthenticatedUser>> AutenticarAsync(LoginRequest requisicao, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(requisicao.Username) || string.IsNullOrWhiteSpace(requisicao.Password))
        {
            return Task.FromResult(Result<AuthenticatedUser>.Validation([new ResultError("credenciais_invalidas", "Usuário e senha são obrigatórios.")]));
        }

        if (string.Equals(requisicao.Password, "invalid", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(Result<AuthenticatedUser>.Unauthorized([new ResultError("credenciais_invalidas", "Usuário ou senha inválidos.")]));
        }

        var userId = Math.Abs(requisicao.Username.GetHashCode());
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        var autenticado = new AuthenticatedUser(userId, requisicao.Username, token, DateTimeOffset.UtcNow.AddHours(1));
        return Task.FromResult(Result<AuthenticatedUser>.Success(autenticado));
    }
}