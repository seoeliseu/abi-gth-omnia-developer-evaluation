using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM.Mongo;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.Sales.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace Ambev.DeveloperEvaluation.Integration.Sales;

public sealed class SalesPersistenceIntegrationTests : IAsyncLifetime
{
    private const string MongoDatabaseName = "developer-evaluation-integration";

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("developer_evaluation_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder("mongo:7.0")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _mongoDbContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _mongoDbContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task CriarAsync_DevePersistirVenda_IdempotenciaEAuditoria()
    {
        await using var provider = CreateSalesProvider();
        await provider.InicializarPersistenciaAsync();

        await using var scope = provider.CreateAsyncScope();
        var salesService = scope.ServiceProvider.GetRequiredService<ISalesApplicationService>();
        var requisicao = new CreateSaleRequest(
            Numero: $"VENDA-{DateTime.UtcNow:yyyyMMddHHmmss}",
            DataVenda: DateTimeOffset.UtcNow,
            ClienteId: 1,
            FilialId: 10,
            FilialNome: "Filial Centro",
            Itens:
            [
                new CreateSaleItemRequest(1, 2),
                new CreateSaleItemRequest(2, 4)
            ]);

        var resultado = await salesService.CriarAsync(requisicao, "idem-sales-create-001", CancellationToken.None);
        var resultadoIdempotente = await salesService.CriarAsync(requisicao, "idem-sales-create-001", CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);
        Assert.True(resultadoIdempotente.IsSuccess);
        Assert.NotNull(resultadoIdempotente.Value);
        Assert.Equal(resultado.Value!.Id, resultadoIdempotente.Value!.Id);
        Assert.Equal(resultado.Value.ValorTotal, resultadoIdempotente.Value.ValorTotal);

        await using var context = CreateDbContext();
        var vendasPersistidas = await context.Sales.Include(venda => venda.Items).ToListAsync();
        var chavesIdempotencia = await context.IdempotencyEntries.Where(entry => entry.Scope == "sales:create").ToListAsync();
        var eventosOutbox = await context.OutboxMessages.Where(message => message.AggregateId == resultado.Value.Id.ToString("N")).ToListAsync();

        Assert.Single(vendasPersistidas);
        Assert.Single(chavesIdempotencia);
        Assert.Contains(eventosOutbox, message => message.EventType == "SaleCreatedEvent");

        var mongoDatabase = provider.GetRequiredService<IMongoDatabase>();
        var auditorias = (await mongoDatabase
            .GetCollection<SaleAuditDocument>("sale_audit")
            .Find(FilterDefinition<SaleAuditDocument>.Empty)
            .ToListAsync())
            .Where(documento => documento.SaleId == resultado.Value.Id)
            .ToList();

        Assert.Contains(auditorias, auditoria => auditoria.EventType == "SaleCreatedEvent");
        Assert.All(auditorias, auditoria => Assert.Equal(requisicao.Numero, auditoria.NumeroVenda));
    }

    [Fact]
    public async Task AtualizarAsync_DevePersistirEventoDeModificacaoEAuditoria()
    {
        await using var provider = CreateSalesProvider();
        await provider.InicializarPersistenciaAsync();

        await using var scope = provider.CreateAsyncScope();
        var salesService = scope.ServiceProvider.GetRequiredService<ISalesApplicationService>();
        var criacao = await salesService.CriarAsync(CriarRequisicaoVenda($"VENDA-UPD-{DateTime.UtcNow:yyyyMMddHHmmss}"), "idem-sales-update-create", CancellationToken.None);

        Assert.True(criacao.IsSuccess);
        Assert.NotNull(criacao.Value);

        var requisicaoAtualizacao = new UpdateSaleRequest(
            DateTimeOffset.UtcNow.AddDays(1),
            1,
            11,
            "Filial Norte",
            [
                new UpdateSaleItemRequest(1, 3),
                new UpdateSaleItemRequest(2, 1)
            ]);

        var atualizacao = await salesService.AtualizarAsync(criacao.Value!.Id, requisicaoAtualizacao, CancellationToken.None);

        Assert.True(atualizacao.IsSuccess);
        Assert.NotNull(atualizacao.Value);
        Assert.Equal("Filial Norte", atualizacao.Value!.FilialNome);

        await using var context = CreateDbContext();
        var eventosOutbox = await context.OutboxMessages.Where(message => message.AggregateId == criacao.Value.Id.ToString("N")).ToListAsync();

        Assert.Contains(eventosOutbox, message => message.EventType == "SaleModifiedEvent");

        var mongoDatabase = provider.GetRequiredService<IMongoDatabase>();
        var auditorias = (await mongoDatabase
            .GetCollection<SaleAuditDocument>("sale_audit")
            .Find(FilterDefinition<SaleAuditDocument>.Empty)
            .ToListAsync())
            .Where(documento => documento.SaleId == criacao.Value.Id)
            .ToList();

        Assert.Contains(auditorias, auditoria => auditoria.EventType == "SaleModifiedEvent");
    }

    [Fact]
    public async Task Cancelamentos_DevenPersistirEventosAuditoriaEIdempotencia()
    {
        await using var provider = CreateSalesProvider();
        await provider.InicializarPersistenciaAsync();

        await using var scope = provider.CreateAsyncScope();
        var salesService = scope.ServiceProvider.GetRequiredService<ISalesApplicationService>();
        var criacao = await salesService.CriarAsync(CriarRequisicaoVenda($"VENDA-CANCEL-{DateTime.UtcNow:yyyyMMddHHmmss}"), "idem-sales-cancel-create", CancellationToken.None);

        Assert.True(criacao.IsSuccess);
        Assert.NotNull(criacao.Value);

        var itemId = criacao.Value!.Itens.First().Id;

        var cancelamentoItem = await salesService.CancelarItemAsync(criacao.Value.Id, itemId, "idem-sales-item-cancel", CancellationToken.None);
        var cancelamentoItemIdempotente = await salesService.CancelarItemAsync(criacao.Value.Id, itemId, "idem-sales-item-cancel", CancellationToken.None);
        var cancelamentoVenda = await salesService.CancelarVendaAsync(criacao.Value.Id, "idem-sales-cancel", CancellationToken.None);
        var cancelamentoVendaIdempotente = await salesService.CancelarVendaAsync(criacao.Value.Id, "idem-sales-cancel", CancellationToken.None);

        Assert.True(cancelamentoItem.IsSuccess);
        Assert.True(cancelamentoItemIdempotente.IsSuccess);
        Assert.True(cancelamentoVenda.IsSuccess);
        Assert.True(cancelamentoVendaIdempotente.IsSuccess);
        Assert.NotNull(cancelamentoVenda.Value);
        Assert.True(cancelamentoVenda.Value!.Cancelada);

        await using var context = CreateDbContext();
        var eventosOutbox = await context.OutboxMessages.Where(message => message.AggregateId == criacao.Value.Id.ToString("N")).ToListAsync();
        var chavesIdempotencia = await context.IdempotencyEntries
            .Where(entry => entry.Key == "idem-sales-item-cancel" || entry.Key == "idem-sales-cancel")
            .ToListAsync();

        Assert.Contains(eventosOutbox, message => message.EventType == "ItemCancelledEvent");
        Assert.Contains(eventosOutbox, message => message.EventType == "SaleCancelledEvent");
        Assert.Single(chavesIdempotencia, entry => entry.Scope == "sales:cancel-item");
        Assert.Single(chavesIdempotencia, entry => entry.Scope == "sales:cancel");

        var mongoDatabase = provider.GetRequiredService<IMongoDatabase>();
        var auditorias = (await mongoDatabase
            .GetCollection<SaleAuditDocument>("sale_audit")
            .Find(FilterDefinition<SaleAuditDocument>.Empty)
            .ToListAsync())
            .Where(documento => documento.SaleId == criacao.Value.Id)
            .ToList();

        Assert.Contains(auditorias, auditoria => auditoria.EventType == "ItemCancelledEvent");
        Assert.Contains(auditorias, auditoria => auditoria.EventType == "SaleCancelledEvent");
    }

    private static CreateSaleRequest CriarRequisicaoVenda(string numero)
    {
        return new CreateSaleRequest(
            Numero: numero,
            DataVenda: DateTimeOffset.UtcNow,
            ClienteId: 1,
            FilialId: 10,
            FilialNome: "Filial Centro",
            Itens:
            [
                new CreateSaleItemRequest(1, 2),
                new CreateSaleItemRequest(2, 4)
            ]);
    }

    private ServiceProvider CreateSalesProvider()
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Postgres"] = _postgreSqlContainer.GetConnectionString();
        configuration["ConnectionStrings:MongoDb"] = _mongoDbContainer.GetConnectionString();
        configuration["MongoDb:Database"] = MongoDatabaseName;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarInfraestruturaCompartilhada(configuration);
        services.AdicionarServicosAplicacaoSales();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private DeveloperEvaluationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DeveloperEvaluationDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString(), npgsql =>
                npgsql.MigrationsAssembly(typeof(DeveloperEvaluationDbContext).Assembly.FullName))
            .Options;

        return new DeveloperEvaluationDbContext(options);
    }
}