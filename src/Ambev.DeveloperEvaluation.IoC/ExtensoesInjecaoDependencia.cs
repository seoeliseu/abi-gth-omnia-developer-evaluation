using Ambev.DeveloperEvaluation.Auth.Infrastructure.DependencyInjection;
using Ambev.DeveloperEvaluation.Carts.Infrastructure.DependencyInjection;
using Ambev.DeveloperEvaluation.Common.Resilience;
using Ambev.DeveloperEvaluation.IoC.Mensageria;
using Ambev.DeveloperEvaluation.IoC.Resilience;
using Ambev.DeveloperEvaluation.ORM.HealthChecks;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.Products.Infrastructure.DependencyInjection;
using Ambev.DeveloperEvaluation.Sales.Domain.Events;
using Ambev.DeveloperEvaluation.Sales.Infrastructure.DependencyInjection;
using Ambev.DeveloperEvaluation.Users.Infrastructure.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.ServiceProvider;

namespace Ambev.DeveloperEvaluation.IoC;

public static class ExtensoesInjecaoDependencia
{
    public static IServiceCollection AdicionarServicosTransversais(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AdicionarInfraestruturaCompartilhada(configuracao);
        servicos.AdicionarServicosAplicacaoSales();
        servicos.AdicionarServicosAplicacaoProducts();
        servicos.AdicionarServicosAplicacaoUsers();
        servicos.AdicionarServicosAplicacaoCarts();
        servicos.AdicionarServicosAplicacaoAuth();
        servicos.TryAdicionarTransporteMensageria(configuracao);

        return servicos;
    }

    public static IServiceCollection AdicionarInfraestruturaCompartilhada(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.AdicionarResilienciaIntegracoes();
        servicos.AdicionarInfraestruturaPersistencia(configuracao);
        servicos.AdicionarHealthChecks();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoSales(this IServiceCollection servicos)
    {
        servicos.AdicionarModuloSales();
        servicos.AdicionarModuloProducts();
        servicos.AdicionarModuloUsers();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoProducts(this IServiceCollection servicos)
    {
        servicos.AdicionarModuloProducts();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoUsers(this IServiceCollection servicos)
    {
        servicos.AdicionarModuloUsers();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoCarts(this IServiceCollection servicos)
    {
        servicos.AdicionarModuloCarts();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoAuth(this IServiceCollection servicos)
    {
        servicos.AdicionarModuloAuth();
        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaSales(this IServiceCollection servicos, IConfiguration configuracao)
    {
        if (servicos.TryAdicionarTransporteMensageria(configuracao))
        {
            servicos.AddHostedService<SalesOutboxPublisherWorker>();
        }

        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaProducts(this IServiceCollection servicos, IConfiguration configuracao)
    {
        if (servicos.TryAdicionarTransporteMensageria(configuracao))
        {
            servicos.AdicionarConsumerEventosVenda<ProductsSalesIntegrationEventsConsumer>();
            servicos.AddHostedService<ProductsSalesEventsSubscriberHostedService>();
        }

        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaUsers(this IServiceCollection servicos, IConfiguration configuracao)
    {
        if (servicos.TryAdicionarTransporteMensageria(configuracao))
        {
            servicos.AdicionarConsumerEventosVenda<UsersSalesIntegrationEventsConsumer>();
            servicos.AddHostedService<UsersSalesEventsSubscriberHostedService>();
        }

        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaCarts(this IServiceCollection servicos, IConfiguration configuracao)
    {
        if (servicos.TryAdicionarTransporteMensageria(configuracao))
        {
            servicos.AdicionarConsumerEventosVenda<CartsSalesIntegrationEventsConsumer>();
            servicos.AddHostedService<CartsSalesEventsSubscriberHostedService>();
        }

        return servicos;
    }

    private static void AdicionarHealthChecks(this IServiceCollection servicos)
    {
        servicos
            .AddHealthChecks()
            .AddCheck("processo_vivo", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck("aplicacao_pronta", () => HealthCheckResult.Healthy(), tags: ["ready"])
            .AddDbContextCheck<DeveloperEvaluationDbContext>("postgresql", tags: ["ready"])
            .AddCheck<MongoReadinessHealthCheck>("mongodb", tags: ["ready"]);
    }

    private static IServiceCollection AdicionarResilienciaIntegracoes(this IServiceCollection servicos)
    {
        servicos.AddSingleton<IIntegrationResilienceExecutor, PollyIntegrationResilienceExecutor>();
        return servicos;
    }

    private static bool TryAdicionarTransporteMensageria(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var stringConexao = configuracao["RabbitMq:ConnectionString"];
        var nomeFila = configuracao["RabbitMq:QueueName"];

        if (string.IsNullOrWhiteSpace(stringConexao) || string.IsNullOrWhiteSpace(nomeFila))
        {
            return false;
        }

        servicos.AddRebus(
            configurador => configurador.Transport(t => t.UseRabbitMq(stringConexao, nomeFila))
        );

        return true;
    }

    private static IServiceCollection AdicionarConsumerEventosVenda<TConsumer>(this IServiceCollection servicos)
        where TConsumer : class,
        IHandleMessages<SaleCreatedEvent>,
        IHandleMessages<SaleModifiedEvent>,
        IHandleMessages<SaleCancelledEvent>,
        IHandleMessages<ItemCancelledEvent>
    {
        servicos.AddTransient<TConsumer>();
        servicos.AddTransient<IHandleMessages<SaleCreatedEvent>>(provider => provider.GetRequiredService<TConsumer>());
        servicos.AddTransient<IHandleMessages<SaleModifiedEvent>>(provider => provider.GetRequiredService<TConsumer>());
        servicos.AddTransient<IHandleMessages<SaleCancelledEvent>>(provider => provider.GetRequiredService<TConsumer>());
        servicos.AddTransient<IHandleMessages<ItemCancelledEvent>>(provider => provider.GetRequiredService<TConsumer>());
        return servicos;
    }
}