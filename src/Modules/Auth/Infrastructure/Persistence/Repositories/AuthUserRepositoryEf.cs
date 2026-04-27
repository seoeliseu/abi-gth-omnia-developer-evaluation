using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Auth.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.Auth.Infrastructure.Persistence.Repositories;

public sealed class AuthUserRepositoryEf : IAuthUserRepository
{
    private readonly DeveloperEvaluationDbContext _context;

    public AuthUserRepositoryEf(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public async Task<AuthUser?> ObterPorNomeUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken)
    {
        var usuario = await _context.Users.AsNoTracking().SingleOrDefaultAsync(item => item.Username == nomeUsuario, cancellationToken);
        return usuario is null ? null : AuthUser.Reidratar(usuario.Id, usuario.Username, usuario.Password);
    }
}
