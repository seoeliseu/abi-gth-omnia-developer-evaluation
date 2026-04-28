using Ambev.DeveloperEvaluation.Auth.WebApi.Controllers;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MongoDb;
using Testcontainers.PostgreSql;

namespace Ambev.DeveloperEvaluation.Functional.Auth;

public sealed class AuthApiFactory : WebApplicationFactory<AuthController>, IAsyncLifetime
{
    private const string DatabaseName = "developer-evaluation-auth-functional";
    private static readonly IReadOnlyDictionary<string, string?> EmptySettings = new Dictionary<string, string?>
    {
        ["ConnectionStrings__Postgres"] = null,
        ["ConnectionStrings__MongoDb"] = null,
        ["MongoDb__Database"] = null,
        ["Jwt__Issuer"] = null,
        ["Jwt__Audience"] = null,
        ["Jwt__SecretKey"] = null,
        ["Jwt__ExpirationMinutes"] = null
    };

    private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder("postgres:15.1")
        .WithDatabase("developer_evaluation_auth_functional")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly MongoDbContainer _mongoDbContainer = new MongoDbBuilder("mongo:7.0")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgreSqlContainer.StartAsync();
        await _mongoDbContainer.StartAsync();

        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings__Postgres"] = _postgreSqlContainer.GetConnectionString(),
            ["ConnectionStrings__MongoDb"] = _mongoDbContainer.GetConnectionString(),
            ["MongoDb__Database"] = DatabaseName
        };

        foreach (var setting in FunctionalJwtSettings.EnvironmentVariables)
        {
            settings[setting.Key] = setting.Value;
        }

        ApplyEnvironmentSettings(settings);
    }

    public new async Task DisposeAsync()
    {
        ApplyEnvironmentSettings(EmptySettings);
        await _mongoDbContainer.DisposeAsync();
        await _postgreSqlContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgreSqlContainer.GetConnectionString(),
                ["ConnectionStrings:MongoDb"] = _mongoDbContainer.GetConnectionString(),
                ["MongoDb:Database"] = DatabaseName
            };

            foreach (var setting in FunctionalJwtSettings.ConfigurationValues)
            {
                settings[setting.Key] = setting.Value;
            }

            configurationBuilder.AddInMemoryCollection(settings);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = FunctionalJwtTokenFactory.CreateAuthorizationHeader();
    }

    private static void ApplyEnvironmentSettings(IReadOnlyDictionary<string, string?> settings)
    {
        foreach (var setting in settings)
        {
            Environment.SetEnvironmentVariable(setting.Key, setting.Value);
        }
    }
}