using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.Functional.Sales;

public sealed class SalesApiFunctionalTests : IClassFixture<SalesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public SalesApiFunctionalTests(SalesApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task PostSales_DeveCriarVendaEPermitirConsultaComIdempotencia()
    {
        var requisicao = new
        {
            numero = $"VENDA-FUNC-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            dataVenda = DateTimeOffset.UtcNow,
            clienteId = 1,
            filialId = 10,
            filialNome = "Filial Centro",
            itens = new[]
            {
                new { productId = 1L, quantidade = 2 },
                new { productId = 2L, quantidade = 4 }
            }
        };

        using var primeiraRequisicao = new HttpRequestMessage(HttpMethod.Post, "/api/sales")
        {
            Content = JsonContent.Create(requisicao)
        };
        primeiraRequisicao.Headers.Add("Idempotency-Key", "sales-functional-idem-001");

        using var primeiraResposta = await _client.SendAsync(primeiraRequisicao);
        var primeiraVenda = await primeiraResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, primeiraResposta.StatusCode);
        Assert.NotNull(primeiraVenda);
        Assert.Equal("sales-functional-idem-001", primeiraResposta.Headers.GetValues("Idempotency-Key").Single());

        using var segundaRequisicao = new HttpRequestMessage(HttpMethod.Post, "/api/sales")
        {
            Content = JsonContent.Create(requisicao)
        };
        segundaRequisicao.Headers.Add("Idempotency-Key", "sales-functional-idem-001");

        using var segundaResposta = await _client.SendAsync(segundaRequisicao);
        var segundaVenda = await segundaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, segundaResposta.StatusCode);
        Assert.NotNull(segundaVenda);
        Assert.Equal(primeiraVenda!.Id, segundaVenda!.Id);
        Assert.Equal(primeiraVenda.ValorTotal, segundaVenda.ValorTotal);

        using var consultaResposta = await _client.GetAsync($"/api/sales/{primeiraVenda.Id}");
        var vendaConsultada = await consultaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, consultaResposta.StatusCode);
        Assert.NotNull(vendaConsultada);
        Assert.Equal(primeiraVenda.Id, vendaConsultada!.Id);
        Assert.Equal(requisicao.numero, vendaConsultada.Numero);
        Assert.Equal(2, vendaConsultada.Itens.Count);
    }

    [Fact]
    public async Task DeleteSales_DeveExcluirVendaECausarNotFoundNaConsulta()
    {
        var requisicao = new
        {
            numero = $"VENDA-DEL-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            dataVenda = DateTimeOffset.UtcNow,
            clienteId = 1,
            filialId = 10,
            filialNome = "Filial Centro",
            itens = new[]
            {
                new { productId = 1L, quantidade = 2 }
            }
        };

        using var criacaoResposta = await _client.PostAsJsonAsync("/api/sales", requisicao);
        var venda = await criacaoResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criacaoResposta.StatusCode);
        Assert.NotNull(venda);

        using var remocaoResposta = await _client.DeleteAsync($"/api/sales/{venda!.Id}");
        using var consultaResposta = await _client.GetAsync($"/api/sales/{venda.Id}");

        Assert.Equal(HttpStatusCode.OK, remocaoResposta.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, consultaResposta.StatusCode);
    }

    private sealed record SaleDetailResponse(Guid Id, string Numero, decimal ValorTotal, IReadOnlyCollection<SaleItemResponse> Itens);

    private sealed record SaleItemResponse(Guid Id, long ProductId, int Quantidade, decimal ValorTotal, bool Cancelado);
}