using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Auth.Application.Services;
using Ambev.DeveloperEvaluation.Auth.Domain.Entities;
using Ambev.DeveloperEvaluation.Common.Security;

namespace Ambev.DeveloperEvaluation.Unit.Auth;

public sealed class AuthApplicationServiceTests
{
    [Fact]
    public async Task AutenticarAsync_DeveRetornarTokenQuandoCredenciaisForemValidas()
    {
        var passwordSecurityService = new PasswordSecurityService();
        var service = new AuthApplicationService(
            new FakeAuthUserRepository(AuthUser.Reidratar(7, "john", passwordSecurityService.HashPassword("123456"))),
            passwordSecurityService,
            new FakeAccessTokenIssuer());

        var resultado = await service.AutenticarAsync("john", "123456", CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Equal("fake.jwt.token", resultado.Value?.Token);
        Assert.Equal(7, resultado.Value?.UsuarioId);
    }

    [Fact]
    public async Task AutenticarAsync_DeveRejeitarSenhaInvalida()
    {
        var passwordSecurityService = new PasswordSecurityService();
        var service = new AuthApplicationService(
            new FakeAuthUserRepository(AuthUser.Reidratar(7, "john", passwordSecurityService.HashPassword("123456"))),
            passwordSecurityService,
            new FakeAccessTokenIssuer());

        var resultado = await service.AutenticarAsync("john", "errada", CancellationToken.None);

        Assert.False(resultado.IsSuccess);
        Assert.Equal("credenciais_invalidas", Assert.Single(resultado.Errors).Codigo);
    }

    private sealed class FakeAuthUserRepository : IAuthUserRepository
    {
        private readonly AuthUser? _user;

        public FakeAuthUserRepository(AuthUser? user)
        {
            _user = user;
        }

        public Task<AuthUser?> ObterPorNomeUsuarioAsync(string nomeUsuario, CancellationToken cancellationToken)
        {
            return Task.FromResult(_user is not null && string.Equals(_user.NomeUsuario, nomeUsuario, StringComparison.Ordinal) ? _user : null);
        }
    }

    private sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
    {
        public AccessToken IssueToken(long usuarioId, string nomeUsuario)
        {
            return new AccessToken("fake.jwt.token", DateTimeOffset.UtcNow.AddHours(1));
        }
    }
}