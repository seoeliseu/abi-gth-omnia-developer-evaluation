using System.Text.Json;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Sales.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rebus.Bus;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Ambev.DeveloperEvaluation.Integration.Messaging;

public sealed class SalesMessagingIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("developer_evaluation_integration")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3.11")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        await using var context = CreateDbContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
    }

    [Fact]
    public async Task SalesOutbox_DevePublicarEvento_E_ConsumerDeveDeduplicarMensagem()
    {
        var queueSuffix = Guid.NewGuid().ToString("N");
        var consumerProvider = CreateProductsConsumerProvider(queueSuffix);
        var publisherProvider = CreateSalesPublisherProvider(queueSuffix);

        try
        {
            await StartHostedServicesAsync(consumerProvider);

            var saleId = Guid.NewGuid();
            var outboxId = Guid.NewGuid();
            var numeroVenda = $"VENDA-{DateTime.UtcNow:yyyyMMddHHmmss}";
            await SeedOutboxMessageAsync(outboxId, saleId, numeroVenda);

            await StartHostedServicesAsync(publisherProvider);

            await WaitUntilAsync(async () =>
            {
                await using var verificationContext = CreateDbContext();
                var outboxMessage = await verificationContext.OutboxMessages.SingleAsync(message => message.Id == outboxId);
                var processedCount = await verificationContext.ProcessedMessages.CountAsync(message => message.Consumer == "products.sales-events");
                return outboxMessage.PublishedAt is not null && processedCount == 1;
            }, TimeSpan.FromSeconds(20));

            var bus = publisherProvider.GetRequiredService<IBus>();
            await bus.Publish(new SaleCreatedEvent(saleId, numeroVenda), new Dictionary<string, string>
            {
                ["outbox-id"] = outboxId.ToString("N"),
                ["aggregate-type"] = "Sale",
                ["aggregate-id"] = saleId.ToString("N"),
                ["event-type"] = nameof(SaleCreatedEvent)
            });

            await Task.Delay(TimeSpan.FromSeconds(2));

            await using var finalContext = CreateDbContext();
            var finalOutboxMessage = await finalContext.OutboxMessages.SingleAsync(message => message.Id == outboxId);
            var processedMessages = await finalContext.ProcessedMessages
                .Where(message => message.Consumer == "products.sales-events")
                .ToListAsync();

            Assert.NotNull(finalOutboxMessage.PublishedAt);
            Assert.Single(processedMessages);
            Assert.Equal(outboxId.ToString("N"), processedMessages[0].MessageId);
        }
        finally
        {
            await StopHostedServicesAsync(publisherProvider);
            await StopHostedServicesAsync(consumerProvider);

            await publisherProvider.DisposeAsync();
            await consumerProvider.DisposeAsync();
        }
    }

    private ServiceProvider CreateProductsConsumerProvider(string queueSuffix)
    {
        var configuration = CreateConfiguration($"developer-evaluation.products.integration.{queueSuffix}");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarInfraestruturaCompartilhada(configuration);
        services.AdicionarMensageriaProducts(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private ServiceProvider CreateSalesPublisherProvider(string queueSuffix)
    {
        var configuration = CreateConfiguration($"developer-evaluation.sales.integration.{queueSuffix}");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AdicionarInfraestruturaCompartilhada(configuration);
        services.AdicionarMensageriaSales(configuration);
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    }

    private ConfigurationManager CreateConfiguration(string queueName)
    {
        var configuration = new ConfigurationManager();
        configuration["ConnectionStrings:Postgres"] = _postgreSqlContainer.GetConnectionString();
        configuration["ConnectionStrings:MongoDb"] = "mongodb://localhost:27017/developer-evaluation-tests";
        configuration["RabbitMq:ConnectionString"] = _rabbitMqContainer.GetConnectionString();
        configuration["RabbitMq:QueueName"] = queueName;
        return configuration;
    }

    private DeveloperEvaluationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DeveloperEvaluationDbContext>()
            .UseNpgsql(_postgreSqlContainer.GetConnectionString(), npgsql =>
                npgsql.MigrationsAssembly(typeof(DeveloperEvaluationDbContext).Assembly.FullName))
            .Options;

        return new DeveloperEvaluationDbContext(options);
    }

    private async Task SeedOutboxMessageAsync(Guid outboxId, Guid saleId, string numeroVenda)
    {
        await using var context = CreateDbContext();
        context.OutboxMessages.Add(new OutboxMessageEntity
        {
            Id = outboxId,
            AggregateType = "Sale",
            AggregateId = saleId.ToString("N"),
            EventType = nameof(SaleCreatedEvent),
            Payload = JsonSerializer.Serialize(new SaleCreatedEvent(saleId, numeroVenda)),
            OccurredAt = DateTimeOffset.UtcNow,
            PublishedAt = null
        });

        await context.SaveChangesAsync();
    }

    private static async Task StartHostedServicesAsync(ServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>())
        {
            await hostedService.StartAsync(CancellationToken.None);
        }
    }

    private static async Task StopHostedServicesAsync(ServiceProvider provider)
    {
        foreach (var hostedService in provider.GetServices<IHostedService>().Reverse())
        {
            await hostedService.StopAsync(CancellationToken.None);
        }
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"Condição não satisfeita dentro do tempo limite de {timeout}.");
    }
}