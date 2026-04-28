using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ambev.DeveloperEvaluation.Functional;

internal static class FunctionalJwtTokenFactory
{
    public static AuthenticationHeaderValue CreateAuthorizationHeader()
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(FunctionalJwtSettings.SecretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var now = DateTime.UtcNow;

        var token = new JwtSecurityToken(
            issuer: FunctionalJwtSettings.Issuer,
            audience: FunctionalJwtSettings.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "1"),
                new Claim(JwtRegisteredClaimNames.UniqueName, "functional-tester"),
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "functional-tester"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: now,
            expires: now.AddMinutes(30),
            signingCredentials: credentials);

        var rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AuthenticationHeaderValue("Bearer", rawToken);
    }
}