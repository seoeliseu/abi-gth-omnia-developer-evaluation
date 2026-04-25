using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

public sealed class AuthApiFunctionalTests : IClassFixture<AuthApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthApiFunctionalTests(AuthApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task PostLogin_DeveAutenticarUsuarioSeedado()
    {
        var resposta = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "john",
            password = "123456"
        });

        var payload = await resposta.Content.ReadFromJsonAsync<TokenResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Token));
    }

    [Fact]
    public async Task PostLogin_DeveRetornarUnauthorizedParaCredenciaisInvalidas()
    {
        var resposta = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "john",
            password = "senha-invalida"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, resposta.StatusCode);
    }

    private sealed record TokenResponse(string Token);
}