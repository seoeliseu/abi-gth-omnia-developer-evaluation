using Ambev.DeveloperEvaluation.Carts.WebApi.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Ambev.DeveloperEvaluation.Functional.Carts;

public sealed class CartsApiFactory : WebApplicationFactory<CartsController>, IAsyncLifetime
{
    private const string DatabaseName = "developer-evaluation-carts-functional";
    private static readonly IReadOnlyDictionary<string, string?> EmptySettings = new Dictionary<string, string?>
    {
        ["ConnectionStrings__Postgres"] = null,
        ["ConnectionStrings__MongoDb"] = null,
        ["MongoDb__Database"] = null,
        ["RabbitMq__ConnectionString"] = null,
        ["RabbitMq__QueueName"] = null
    };

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("developer_evaluation_carts_functional")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder("mongo:7.0")
        .Build();

    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder("rabbitmq:3.13-management")
        .WithUsername("guest")
        .WithPassword("guest")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _mongoDbContainer.StartAsync();
        await _rabbitMqContainer.StartAsync();

        ApplyEnvironmentSettings(new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = _postgreSqlContainer.GetConnectionString(),
            ["ConnectionStrings__MongoDb"] = _mongoDbContainer.GetConnectionString(),
            ["MongoDb__Database"] = DatabaseName,
            ["RabbitMq__ConnectionString"] = _rabbitMqContainer.GetConnectionString(),
            ["RabbitMq__QueueName"] = "developer-evaluation.carts.functional"
        });
    }

    public new async Task DisposeAsync()
    {
        ApplyEnvironmentSettings(EmptySettings);
        await _rabbitMqContainer.DisposeAsync();
        await _mongoDbContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgreSqlContainer.GetConnectionString(),
                ["ConnectionStrings:MongoDb"] = _mongoDbContainer.GetConnectionString(),
                ["MongoDb:Database"] = DatabaseName,
                ["RabbitMq:ConnectionString"] = _rabbitMqContainer.GetConnectionString(),
                ["RabbitMq:QueueName"] = "developer-evaluation.carts.functional"
            });
        });
    }

    private static void ApplyEnvironmentSettings(IReadOnlyDictionary<string, string?> settings)
    {
        foreach (var setting in settings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }
}