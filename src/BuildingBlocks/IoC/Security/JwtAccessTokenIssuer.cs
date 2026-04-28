using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Ambev.DeveloperEvaluation.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ambev.DeveloperEvaluation.IoC.Security;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtOptions _options;
    private readonly SigningCredentials _signingCredentials;

    public JwtAccessTokenIssuer(IConfiguration configuration)
    {
        _options = ResolveOptions(configuration);
        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);
    }

    public AccessToken IssueToken(long usuarioId, string nomeUsuario)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpirationMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuarioId.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, nomeUsuario),
            new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new Claim(ClaimTypes.Name, nomeUsuario),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _signingCredentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public static JwtOptions ResolveOptions(IConfiguration configuration)
    {
        var issuer = GetRequiredValue(configuration, $"{JwtOptions.SectionName}:Issuer");
        var audience = GetRequiredValue(configuration, $"{JwtOptions.SectionName}:Audience");
        var secretKey = GetRequiredValue(configuration, $"{JwtOptions.SectionName}:SecretKey");
        if (Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException("A chave JWT deve ter pelo menos 32 bytes.");
        }

        var expirationMinutes = 60;
        if (int.TryParse(configuration[$"{JwtOptions.SectionName}:ExpirationMinutes"], out var configuredExpirationMinutes))
        {
            expirationMinutes = Math.Max(configuredExpirationMinutes, 1);
        }

        return new JwtOptions
        {
            Issuer = issuer,
            Audience = audience,
            SecretKey = secretKey,
            ExpirationMinutes = expirationMinutes
        };
    }

    private static string GetRequiredValue(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value) || (value.StartsWith("__", StringComparison.Ordinal) && value.EndsWith("__", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"A configuração '{key}' não foi preenchida corretamente.");
        }

        return value;
    }
}