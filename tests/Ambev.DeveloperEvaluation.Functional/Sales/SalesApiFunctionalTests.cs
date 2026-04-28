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
        var requisicao = CriarRequisicaoVenda($"VENDA-FUNC-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

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
        Assert.Equal(requisicao.Numero, vendaConsultada.Numero);
        Assert.Equal(2, vendaConsultada.Itens.Count);
    }

    [Fact]
    public async Task GetSales_DeveListarVendaFiltradaPorNumero()
    {
        var numeroVenda = $"VENDA-LIST-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var requisicao = CriarRequisicaoVenda(numeroVenda);

        using var criarResposta = await _client.PostAsJsonAsync("/api/sales", requisicao);
        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);

        using var listarResposta = await _client.GetAsync($"/api/sales?page=1&size=10&numero={numeroVenda}");
        var vendas = await listarResposta.Content.ReadFromJsonAsync<PagedResponse<SaleListItemResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listarResposta.StatusCode);
        Assert.NotNull(vendas);
        Assert.Contains(vendas!.Data, venda => venda.Numero == numeroVenda);
    }

    [Fact]
    public async Task PutSales_DeveAtualizarVendaExistente()
    {
        var numeroVenda = $"VENDA-UPD-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
        var requisicaoCriacao = CriarRequisicaoVenda(numeroVenda);

        using var criarResposta = await _client.PostAsJsonAsync("/api/sales", requisicaoCriacao);
        var criada = await criarResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criada);

        var requisicaoAtualizacao = new
        {
            dataVenda = requisicaoCriacao.DataVenda.AddDays(1),
            clienteId = requisicaoCriacao.ClienteId,
            filialId = 11L,
            filialNome = "Filial Norte",
            itens = new[]
            {
                new { productId = 1L, quantidade = 3 },
                new { productId = 2L, quantidade = 1 }
            }
        };

        using var atualizarResposta = await _client.PutAsJsonAsync($"/api/sales/{criada!.Id}", requisicaoAtualizacao);
        var atualizada = await atualizarResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, atualizarResposta.StatusCode);
        Assert.NotNull(atualizada);
        Assert.Equal(requisicaoAtualizacao.filialNome, atualizada!.FilialNome);
        Assert.Contains(atualizada.Itens, item => item.ProductId == 1 && item.Quantidade == 3 && !item.Cancelado);
        Assert.Contains(atualizada.Itens, item => item.ProductId == 2 && item.Quantidade == 1 && !item.Cancelado);

        using var consultaResposta = await _client.GetAsync($"/api/sales/{criada.Id}");
        var consultada = await consultaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, consultaResposta.StatusCode);
        Assert.NotNull(consultada);
        Assert.Equal(requisicaoAtualizacao.filialNome, consultada!.FilialNome);
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

    [Fact]
    public async Task PostCancelSales_DeveCancelarVendaComIdempotencia()
    {
        var requisicao = CriarRequisicaoVenda($"VENDA-CANCEL-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        using var criarResposta = await _client.PostAsJsonAsync("/api/sales", requisicao);
        var criada = await criarResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criada);

        using var primeiraRequisicao = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/{criada!.Id}/cancel");
        primeiraRequisicao.Headers.Add("Idempotency-Key", "sales-functional-cancel-001");

        using var primeiraResposta = await _client.SendAsync(primeiraRequisicao);
        var cancelada = await primeiraResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, primeiraResposta.StatusCode);
        Assert.NotNull(cancelada);
        Assert.True(cancelada!.Cancelada);
        Assert.Equal("sales-functional-cancel-001", primeiraResposta.Headers.GetValues("Idempotency-Key").Single());

        using var segundaRequisicao = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/{criada.Id}/cancel");
        segundaRequisicao.Headers.Add("Idempotency-Key", "sales-functional-cancel-001");

        using var segundaResposta = await _client.SendAsync(segundaRequisicao);
        var canceladaIdempotente = await segundaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, segundaResposta.StatusCode);
        Assert.NotNull(canceladaIdempotente);
        Assert.True(canceladaIdempotente!.Cancelada);

        using var consultaResposta = await _client.GetAsync($"/api/sales/{criada.Id}");
        var consultada = await consultaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, consultaResposta.StatusCode);
        Assert.NotNull(consultada);
        Assert.True(consultada!.Cancelada);
    }

    [Fact]
    public async Task PostCancelItemSales_DeveCancelarItemDaVenda()
    {
        var requisicao = CriarRequisicaoVenda($"VENDA-ITEM-{DateTime.UtcNow:yyyyMMddHHmmssfff}");

        using var criarResposta = await _client.PostAsJsonAsync("/api/sales", requisicao);
        var criada = await criarResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criada);

        var item = criada!.Itens.First();

        using var cancelarItemRequisicao = new HttpRequestMessage(HttpMethod.Post, $"/api/sales/{criada.Id}/items/{item.Id}/cancel");
        cancelarItemRequisicao.Headers.Add("Idempotency-Key", "sales-functional-item-cancel-001");

        using var cancelarItemResposta = await _client.SendAsync(cancelarItemRequisicao);
        var atualizada = await cancelarItemResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, cancelarItemResposta.StatusCode);
        Assert.NotNull(atualizada);
        Assert.Contains(atualizada!.Itens, atual => atual.Id == item.Id && atual.Cancelado);

        using var consultaResposta = await _client.GetAsync($"/api/sales/{criada.Id}");
        var consultada = await consultaResposta.Content.ReadFromJsonAsync<SaleDetailResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, consultaResposta.StatusCode);
        Assert.NotNull(consultada);
        Assert.Contains(consultada!.Itens, atual => atual.Id == item.Id && atual.Cancelado);
    }

    private static SaleCreateRequest CriarRequisicaoVenda(string numero)
    {
        return new SaleCreateRequest(
            numero,
            DateTimeOffset.UtcNow,
            1,
            10,
            "Filial Centro",
            [
                new SaleCreateItemRequest(1, 2),
                new SaleCreateItemRequest(2, 4)
            ]);
    }

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, int TotalItems, int CurrentPage, int TotalPages);

    private sealed record SaleCreateRequest(string Numero, DateTimeOffset DataVenda, long ClienteId, long FilialId, string FilialNome, IReadOnlyCollection<SaleCreateItemRequest> Itens);

    private sealed record SaleCreateItemRequest(long ProductId, int Quantidade);

    private sealed record SaleListItemResponse(Guid Id, string Numero, decimal ValorTotal, bool Cancelada);

    private sealed record SaleDetailResponse(Guid Id, string Numero, string FilialNome, decimal ValorTotal, bool Cancelada, IReadOnlyCollection<SaleItemResponse> Itens);

    private sealed record SaleItemResponse(Guid Id, long ProductId, int Quantidade, decimal ValorTotal, bool Cancelado);
}