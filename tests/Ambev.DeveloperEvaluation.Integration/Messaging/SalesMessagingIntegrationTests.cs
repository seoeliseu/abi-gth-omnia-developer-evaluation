using System.Collections.Concurrent;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Mensageria;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.IoC.Mensageria;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Sales.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
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

    [Theory]
    [InlineData("products", "products.sales-events")]
    [InlineData("users", "users.sales-events")]
    [InlineData("carts", "carts.sales-events")]
    public async Task SalesOutbox_DevePublicarEvento_E_ConsumerDeveDeduplicarMensagem(string consumerType, string consumerName)
    {
        var queueSuffix = Guid.NewGuid().ToString("N");
        var consumerProvider = CreateConsumerProvider(consumerType, queueSuffix);
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
                var processedCount = await verificationContext.ProcessedMessages.CountAsync(message => message.Consumer == consumerName && message.MessageId == outboxId.ToString("N"));
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
                .Where(message => message.Consumer == consumerName && message.MessageId == outboxId.ToString("N"))
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

    [Fact]
    public async Task SalesOutbox_ComPoisonMessage_DeveEncaminharParaDlq_E_EmitirLog()
    {
        var queueSuffix = Guid.NewGuid().ToString("N");
        var queueName = $"developer-evaluation.products.integration.{queueSuffix}";
        var errorQueueName = $"{queueName}.error";
        RebusDlqDiagnostics.Clear();

        var consumerProvider = CreateConsumerProvider(
            "products",
            queueSuffix,
            services =>
            {
                services.RemoveAll<IProcessedMessageStore>();
                services.AddScoped<IProcessedMessageStore, PoisonProcessedMessageStore>();
            });

        var publisherProvider = CreateSalesPublisherProvider(queueSuffix);

        try
        {
            await StartHostedServicesAsync(consumerProvider);

            var saleId = Guid.NewGuid();
            var outboxId = Guid.NewGuid();
            await SeedOutboxMessageAsync(outboxId, saleId, $"VENDA-POISON-{DateTime.UtcNow:yyyyMMddHHmmss}");

            await StartHostedServicesAsync(publisherProvider);

            await WaitUntilAsync(async () =>
            {
                await using var verificationContext = CreateDbContext();
                var outboxMessage = await verificationContext.OutboxMessages.SingleAsync(message => message.Id == outboxId);
                var dlqCount = await TryGetQueueMessageCountAsync(errorQueueName);
                return outboxMessage.PublishedAt is not null && dlqCount == 1;
            }, TimeSpan.FromSeconds(30));

            var dlqCount = await TryGetQueueMessageCountAsync(errorQueueName);
            Assert.Equal<uint>(1, dlqCount ?? 0);
            Assert.True(RebusDlqDiagnostics.Contains("Mensagem desviada para DLQ"));
        }
        finally
        {
            await StopHostedServicesAsync(publisherProvider);
            await StopHostedServicesAsync(consumerProvider);

            await publisherProvider.DisposeAsync();
            await consumerProvider.DisposeAsync();
        }
    }

    private ServiceProvider CreateConsumerProvider(
        string consumerType,
        string queueSuffix,
        Action<IServiceCollection>? configureServices = null,
        ILoggerProvider? loggerProvider = null)
    {
        var configuration = CreateConfiguration($"developer-evaluation.{consumerType}.integration.{queueSuffix}");
        var services = new ServiceCollection();
        if (loggerProvider is null)
        {
            services.AddLogging();
        }
        else
        {
            services.AddLogging(builder => builder.AddProvider(loggerProvider));
        }

        services.AdicionarInfraestruturaCompartilhada(configuration);

        switch (consumerType)
        {
            case "products":
                services.AdicionarMensageriaProducts(configuration);
                break;
            case "users":
                services.AdicionarMensageriaUsers(configuration);
                break;
            case "carts":
                services.AdicionarMensageriaCarts(configuration);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(consumerType), consumerType, "Consumidor de teste não suportado.");
        }

        configureServices?.Invoke(services);

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
        configuration["RabbitMq:MaxDeliveryAttempts"] = "3";
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

    private async Task<uint?> TryGetQueueMessageCountAsync(string queueName)
    {
        try
        {
            var connectionFactory = new ConnectionFactory
            {
                Uri = new Uri(_rabbitMqContainer.GetConnectionString())
            };

            await using var connection = await connectionFactory.CreateConnectionAsync(CancellationToken.None);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: CancellationToken.None);
            var queue = await channel.QueueDeclarePassiveAsync(queueName, CancellationToken.None);
            return queue.MessageCount;
        }
        catch
        {
            return null;
        }
    }
}

public sealed class PoisonProcessedMessageStore : IProcessedMessageStore
{
    public Task<bool> JaProcessadaAsync(string consumidor, string messageId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task RegistrarAsync(string consumidor, string messageId, CancellationToken cancellationToken)
    {
        throw new InvalidOperationException("Poison message de teste para validar a DLQ do Rebus.");
    }
}

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new InMemoryLogger(categoryName, _entries);
    }

    public bool Contains(LogLevel logLevel, string messageFragment)
    {
        return _entries.Any(entry =>
            entry.LogLevel == logLevel
            && entry.Message.Contains(messageFragment, StringComparison.Ordinal));
    }

    public void Dispose()
    {
    }

    private sealed class InMemoryLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly ConcurrentQueue<CapturedLogEntry> _entries;

        public InMemoryLogger(string categoryName, ConcurrentQueue<CapturedLogEntry> entries)
        {
            _categoryName = categoryName;
            _entries = entries;
        }

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _entries.Enqueue(new CapturedLogEntry(_categoryName, logLevel, formatter(state, exception)));
        }
    }

    private sealed record CapturedLogEntry(string CategoryName, LogLevel LogLevel, string Message);

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}