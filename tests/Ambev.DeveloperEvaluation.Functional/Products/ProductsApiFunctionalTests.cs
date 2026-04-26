using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.Functional.Products;

public sealed class ProductsApiFunctionalTests : IClassFixture<ProductsApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public ProductsApiFunctionalTests(ProductsApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetProducts_DeveListarProdutosSeedadosECategorias()
    {
        using var produtosResposta = await _client.GetAsync("/api/products?page=1&size=10");
        var produtos = await produtosResposta.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>(JsonOptions);

        using var categoriasResposta = await _client.GetAsync("/api/products/categories");
        var categorias = await categoriasResposta.Content.ReadFromJsonAsync<IReadOnlyCollection<string>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, produtosResposta.StatusCode);
        Assert.NotNull(produtos);
        Assert.True(produtos!.TotalItems >= 3);
        Assert.Contains(produtos.Data, produto => produto.Title == "Premium Beer Box");

        Assert.Equal(HttpStatusCode.OK, categoriasResposta.StatusCode);
        Assert.NotNull(categorias);
        Assert.Contains("beverage", categorias!);
    }

    [Fact]
    public async Task PostProducts_DeveCriarProdutoEPermitirConsulta()
    {
        var requisicao = new
        {
            title = "Energy Drink Pack",
            price = 24.90m,
            description = "Pack promocional de bebidas energéticas",
            category = "beverage",
            image = "https://example.com/products/energy-pack.png",
            rating = new
            {
                rate = 4.6m,
                count = 18
            }
        };

        using var criarResposta = await _client.PostAsJsonAsync("/api/products", requisicao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);
        Assert.Equal(requisicao.title, criado!.Title);
        Assert.Equal(requisicao.category, criado.Category);

        using var obterResposta = await _client.GetAsync($"/api/products/{criado.Id}");
        var obtido = await obterResposta.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, obterResposta.StatusCode);
        Assert.NotNull(obtido);
        Assert.Equal(criado.Id, obtido!.Id);
        Assert.Equal(requisicao.title, obtido.Title);
        Assert.Equal(requisicao.price, obtido.Price);
    }

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, int TotalItems, int CurrentPage, int TotalPages);

    private sealed record ProductResponse(long Id, string Title, decimal Price, string Description, string Category, string Image, RatingResponse Rating, bool Active);

    private sealed record RatingResponse(decimal Rate, int Count);
}