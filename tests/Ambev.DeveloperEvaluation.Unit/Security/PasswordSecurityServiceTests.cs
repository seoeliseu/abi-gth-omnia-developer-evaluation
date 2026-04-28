using Ambev.DeveloperEvaluation.Common.Security;

namespace Ambev.DeveloperEvaluation.Unit.Security;

public sealed class PasswordSecurityServiceTests
{
    private readonly PasswordSecurityService _service = new();

    [Fact]
    public void HashPassword_DeveGerarHashVerificavel()
    {
        var hash = _service.HashPassword("senha-forte-123");

        Assert.NotEqual("senha-forte-123", hash);
        Assert.True(_service.VerifyPassword("senha-forte-123", hash));
        Assert.False(_service.NeedsRehash(hash));
    }

    [Fact]
    public void VerifyPassword_DeveRejeitarSenhaInvalida()
    {
        var hash = _service.HashPassword("senha-forte-123");

        Assert.False(_service.VerifyPassword("outra-senha", hash));
    }

    [Fact]
    public void VerifyPassword_DeveAceitarFormatoLegadoEmTextoPuro()
    {
        Assert.True(_service.VerifyPassword("123456", "123456"));
        Assert.True(_service.NeedsRehash("123456"));
    }
}