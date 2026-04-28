using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class AuthServicePersistente : IAuthService
{
    private readonly DeveloperEvaluationDbContext _context;
    private readonly IPasswordSecurityService _passwordSecurityService;
    private readonly IAccessTokenIssuer _accessTokenIssuer;

    public AuthServicePersistente(
        DeveloperEvaluationDbContext context,
        IPasswordSecurityService passwordSecurityService,
        IAccessTokenIssuer accessTokenIssuer)
    {
        _context = context;
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

        var usuario = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Username == requisicao.Username, cancellationToken);
        if (usuario is null || !_passwordSecurityService.VerifyPassword(requisicao.Password, usuario.Password))
        {
            return Result<AuthenticatedUser>.Unauthorized([new ResultError("credenciais_invalidas", "Usuário ou senha inválidos.")]);
        }

        var token = _accessTokenIssuer.IssueToken(usuario.Id, usuario.Username);
        return Result<AuthenticatedUser>.Success(new AuthenticatedUser(usuario.Id, usuario.Username, token.Token, token.ExpiresAt));
    }
}