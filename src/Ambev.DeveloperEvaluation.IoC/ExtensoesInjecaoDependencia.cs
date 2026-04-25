using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Application.Auth.Contracts;
using Ambev.DeveloperEvaluation.Application.Carts.Contracts;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Services;
using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.IoC.Mensageria;
using Ambev.DeveloperEvaluation.ORM.HealthChecks;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Rebus.Config;
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
        servicos.AdicionarInfraestruturaPersistencia(configuracao);
        servicos.AdicionarHealthChecks();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoSales(this IServiceCollection servicos)
    {
        servicos.AdicionarPersistenciaSales();
        servicos.AdicionarPersistenciaProducts();
        servicos.AdicionarPersistenciaUsers();
        servicos.AddScoped<ISalesApplicationService, SalesApplicationService>();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoProducts(this IServiceCollection servicos)
    {
        servicos.AdicionarPersistenciaProducts();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoUsers(this IServiceCollection servicos)
    {
        servicos.AdicionarPersistenciaUsers();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoCarts(this IServiceCollection servicos)
    {
        servicos.AdicionarPersistenciaCarts();
        return servicos;
    }

    public static IServiceCollection AdicionarServicosAplicacaoAuth(this IServiceCollection servicos)
    {
        servicos.AdicionarPersistenciaAuth();
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
        servicos.TryAdicionarTransporteMensageria(configuracao);
        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaUsers(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.TryAdicionarTransporteMensageria(configuracao);
        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaCarts(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.TryAdicionarTransporteMensageria(configuracao);
        return servicos;
    }

    public static IServiceCollection AdicionarMensageriaAuth(this IServiceCollection servicos, IConfiguration configuracao)
    {
        servicos.TryAdicionarTransporteMensageria(configuracao);
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
}