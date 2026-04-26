using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.Functional.Carts;

public sealed class CartsApiFunctionalTests : IClassFixture<CartsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public CartsApiFunctionalTests(CartsApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetCarts_DeveListarCarrinhosSeedados()
    {
        using var resposta = await _client.GetAsync("/api/carts?page=1&size=10");
        var carrinhos = await resposta.Content.ReadFromJsonAsync<PagedResponse<CartResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(carrinhos);
        Assert.True(carrinhos!.TotalItems >= 2);
        Assert.Contains(carrinhos.Data, carrinho => carrinho.UsuarioId == 1 && carrinho.Produtos.Any(produto => produto.ProductId == 1));
    }

    [Fact]
    public async Task PostCarts_DeveCriarCarrinhoEPermitirConsulta()
    {
        var data = new DateTimeOffset(2026, 4, 25, 12, 30, 0, TimeSpan.Zero);
        var requisicao = new
        {
            userId = 2L,
            date = data,
            products = new[]
            {
                new { productId = 1L, quantidade = 1 },
                new { productId = 2L, quantidade = 3 }
            }
        };

        using var criarResposta = await _client.PostAsJsonAsync("/api/carts", requisicao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<CartResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);
        Assert.Equal(requisicao.userId, criado!.UsuarioId);
        Assert.Equal(2, criado.Produtos.Count);
        Assert.Contains(criado.Produtos, produto => produto.ProductId == 2 && produto.Quantidade == 3);

        using var obterResposta = await _client.GetAsync($"/api/carts/{criado.Id}");
        var obtido = await obterResposta.Content.ReadFromJsonAsync<CartResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, obterResposta.StatusCode);
        Assert.NotNull(obtido);
        Assert.Equal(criado.Id, obtido!.Id);
        Assert.Equal(requisicao.userId, obtido.UsuarioId);
        Assert.Equal(data, obtido.Data);
    }

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, int TotalItems, int CurrentPage, int TotalPages);

    private sealed record CartResponse(long Id, long UsuarioId, DateTimeOffset Data, IReadOnlyCollection<CartItemResponse> Produtos);

    private sealed record CartItemResponse(long ProductId, int Quantidade);
}