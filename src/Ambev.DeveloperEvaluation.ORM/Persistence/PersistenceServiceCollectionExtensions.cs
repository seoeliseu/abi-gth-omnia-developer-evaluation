using Ambev.DeveloperEvaluation.Application.Auth.Contracts;
using Ambev.DeveloperEvaluation.Application.Carts.Contracts;
using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Application.Common.Mensageria;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Repositories;
using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.ORM.HealthChecks;
using Ambev.DeveloperEvaluation.ORM.Mongo;
using Ambev.DeveloperEvaluation.ORM.Persistence.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AdicionarPersistencia(this IServiceCollection servicos, IConfiguration configuracao)
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

        servicos.AddScoped<ISaleRepository, SaleRepositoryEf>();
        servicos.AddScoped<IIdempotencyStore, PostgresIdempotencyStore>();
        servicos.AddScoped<IProductsService, ProductsServicePersistente>();
        servicos.AddScoped<IUsersService, UsersServicePersistente>();
        servicos.AddScoped<ICartsService, CartsServicePersistente>();
        servicos.AddScoped<IAuthService, AuthServicePersistente>();
        servicos.AddScoped<IProcessedMessageStore, ProcessedMessageStorePostgres>();
        servicos.AddScoped<ISaleAuditStore, SaleAuditStoreMongo>();
        servicos.AddScoped<MongoReadinessHealthCheck>();

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