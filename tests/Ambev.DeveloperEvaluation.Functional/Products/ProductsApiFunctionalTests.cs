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

    [Fact]
    public async Task GetProductsPorCategoria_DeveListarSomenteProdutosDaCategoriaInformada()
    {
        using var resposta = await _client.GetAsync("/api/products/category/beverage?page=1&size=10");
        var produtos = await resposta.Content.ReadFromJsonAsync<PagedResponse<ProductResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(produtos);
        Assert.NotEmpty(produtos!.Data);
        Assert.All(produtos.Data, produto => Assert.Equal("beverage", produto.Category));
    }

    [Fact]
    public async Task PutProducts_DeveAtualizarProdutoExistente()
    {
        var requisicaoCriacao = new
        {
            title = "Craft Soda Pack",
            price = 18.40m,
            description = "Kit inicial de refrigerantes artesanais",
            category = "beverage",
            image = "https://example.com/products/craft-soda-pack.png",
            rating = new
            {
                rate = 4.1m,
                count = 8
            }
        };

        using var criarResposta = await _client.PostAsJsonAsync("/api/products", requisicaoCriacao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);

        var requisicaoAtualizacao = new
        {
            title = "Craft Soda Pack XL",
            price = 21.90m,
            description = "Kit ampliado de refrigerantes artesanais",
            category = "beverage",
            image = "https://example.com/products/craft-soda-pack-xl.png",
            rating = new
            {
                rate = 4.8m,
                count = 17
            }
        };

        using var atualizarResposta = await _client.PutAsJsonAsync($"/api/products/{criado!.Id}", requisicaoAtualizacao);
        var atualizado = await atualizarResposta.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, atualizarResposta.StatusCode);
        Assert.NotNull(atualizado);
        Assert.Equal(criado.Id, atualizado!.Id);
        Assert.Equal(requisicaoAtualizacao.title, atualizado.Title);
        Assert.Equal(requisicaoAtualizacao.price, atualizado.Price);
        Assert.Equal(requisicaoAtualizacao.rating.count, atualizado.Rating.Count);
    }

    [Fact]
    public async Task DeleteProducts_DeveRemoverProdutoECausarNotFoundNaConsulta()
    {
        var requisicao = new
        {
            title = "Temporary Product",
            price = 9.90m,
            description = "Produto temporário para teste funcional",
            category = "misc",
            image = "https://example.com/products/temp-product.png",
            rating = new
            {
                rate = 3.9m,
                count = 2
            }
        };

        using var criarResposta = await _client.PostAsJsonAsync("/api/products", requisicao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<ProductResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);

        using var removerResposta = await _client.DeleteAsync($"/api/products/{criado!.Id}");
        Assert.Equal(HttpStatusCode.OK, removerResposta.StatusCode);

        using var obterResposta = await _client.GetAsync($"/api/products/{criado.Id}");
        Assert.Equal(HttpStatusCode.NotFound, obterResposta.StatusCode);
    }

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, int TotalItems, int CurrentPage, int TotalPages);

    private sealed record ProductResponse(long Id, string Title, decimal Price, string Description, string Category, string Image, RatingResponse Rating, bool Active);

    private sealed record RatingResponse(decimal Rate, int Count);
}