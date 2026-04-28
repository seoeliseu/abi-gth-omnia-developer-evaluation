using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.Common.Security;

namespace Ambev.DeveloperEvaluation.Auth.Application.Services;

public sealed class AuthApplicationService : IAuthService
{
    private readonly IAuthUserRepository _authUserRepository;
    private readonly IPasswordSecurityService _passwordSecurityService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;

    public AuthApplicationService(
        IAuthUserRepository authUserRepository,
        IPasswordSecurityService passwordSecurityService,
        IAccessTokenIssuer accessTokenIssuer)
    {
        _authUserRepository = authUserRepository;
        _passwordSecurityService = passwordSecurityService;
        _accessTokenIssuer = accessTokenIssuer;
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
        if (usuario is null || !_passwordSecurityService.VerifyPassword(requisicao.Password, usuario.Senha))
        {
            return Result<AuthenticatedUser>.Unauthorized([new ResultError("credenciais_invalidas", "Usuário ou senha inválidos.")]);
        }

        var token = _accessTokenIssuer.IssueToken(usuario.UsuarioId, usuario.NomeUsuario);
        return Result<AuthenticatedUser>.Success(new AuthenticatedUser(usuario.UsuarioId, usuario.NomeUsuario, token.Token, token.ExpiresAt));
    }
}
