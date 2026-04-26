using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Auth.Application.Services;

public sealed class AuthApplicationService : IAuthService
{
    private readonly IAuthUserRepository _authUserRepository;

    public AuthApplicationService(IAuthUserRepository authUserRepository)
    {
        _authUserRepository = authUserRepository;
    }

    public Task<Result<AuthenticatedUser>> AutenticarAsync(string nomeUsuario, string senha, CancellationToken cancellationToken)
    {
        return AutenticarAsync(new LoginRequest(nomeUsuario, senha), cancellationToken);
    }

    public async Task<Result<AuthenticatedUser>> AutenticarAsync(LoginRequest requisicao, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requisicao.Username) || string.IsNullOrWhiteSpace(requisicao.Password))
        {
            return Result<AuthenticatedUser>.Validation([new ResultError("credenciais_invalidas", "Usuário e senha são obrigatórios.")]);
        }

        var usuario = await _authUserRepository.ObterPorNomeUsuarioAsync(requisicao.Username, cancellationToken);
        if (usuario is null || !string.Equals(usuario.Senha, requisicao.Password, StringComparison.Ordinal))
        {
            return Result<AuthenticatedUser>.Unauthorized([new ResultError("credenciais_invalidas", "Usuário ou senha inválidos.")]);
        }

        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        return Result<AuthenticatedUser>.Success(new AuthenticatedUser(usuario.UsuarioId, usuario.NomeUsuario, token, DateTimeOffset.UtcNow.AddHours(1)));
    }
}
