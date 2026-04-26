using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Mensageria;
using Ambev.DeveloperEvaluation.ORM.HealthChecks;
using Ambev.DeveloperEvaluation.ORM.Mongo;
using Ambev.DeveloperEvaluation.ORM.Persistence.Services;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AdicionarPersistencia(this IServiceCollection servicos, IConfiguration configuracao)
    {
        return servicos
            .AdicionarInfraestruturaPersistencia(configuracao)
            .AdicionarPersistenciaSales()
            .AdicionarPersistenciaProducts()
            .AdicionarPersistenciaUsers()
            .AdicionarPersistenciaCarts()
            .AdicionarPersistenciaAuth();
    }

    public static IServiceCollection AdicionarInfraestruturaPersistencia(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var connectionStringPostgres = configuracao.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("A connection string 'Postgres' não foi configurada.");

        var connectionStringMongo = configuracao.GetConnectionString("MongoDb")
            ?? throw new InvalidOperationException("A connection string 'MongoDb' não foi configurada.");

        servicos.AddDbContext<DeveloperEvaluationDbContext>(options =>
            options.UseNpgsql(connectionStringPostgres, npgsql => npgsql.MigrationsAssembly(typeof(DeveloperEvaluationDbContext).Assembly.FullName)));

        servicos.AddSingleton<IMongoClient>(_ => new MongoClient(connectionStringMongo));
        servicos.AddSingleton(provider =>
        {
            var cliente = provider.GetRequiredService<IMongoClient>();
            var mongoUrl = MongoUrl.Create(connectionStringMongo);
            var databaseName = mongoUrl.DatabaseName ?? configuracao["MongoDb:Database"] ?? "developer-evaluation";
            return cliente.GetDatabase(databaseName);
        });

        servicos.AddScoped<IProcessedMessageStore, ProcessedMessageStorePostgres>();
        servicos.AddScoped<ISaleAuditStore, SaleAuditStoreMongo>();
        servicos.AddScoped<MongoReadinessHealthCheck>();

        return servicos;
    }

    public static IServiceCollection AdicionarPersistenciaSales(this IServiceCollection servicos)
    {
        servicos.AddScoped<IIdempotencyStore, PostgresIdempotencyStore>();
        return servicos;
    }

    public static IServiceCollection AdicionarPersistenciaProducts(this IServiceCollection servicos)
    {
        servicos.AddScoped<IProductsService, ProductsServicePersistente>();
        return servicos;
    }

    public static IServiceCollection AdicionarPersistenciaUsers(this IServiceCollection servicos)
    {
        servicos.AddScoped<IUsersService, UsersServicePersistente>();
        return servicos;
    }

    public static IServiceCollection AdicionarPersistenciaCarts(this IServiceCollection servicos)
    {
        servicos.AddScoped<ICartsService, CartsServicePersistente>();
        return servicos;
    }

    public static IServiceCollection AdicionarPersistenciaAuth(this IServiceCollection servicos)
    {
        servicos.AddScoped<IAuthService, AuthServicePersistente>();
        return servicos;
    }

    public static async Task InicializarPersistenciaAsync(this IServiceProvider provider, CancellationToken cancellationToken = default)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DeveloperEvaluationDbContext>();
        await context.Database.MigrateAsync(cancellationToken);
        await DeveloperEvaluationDataSeeder.SeedAsync(context, cancellationToken);
    }
}