using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Application.Sales.Contracts;
using Ambev.DeveloperEvaluation.Application.Sales.Services;
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
        servicos.AdicionarPersistencia(configuracao);
        servicos.AdicionarServicosAplicacao();
        servicos.AdicionarHealthChecks();
        servicos.AdicionarMensageria(configuracao);

        return servicos;
    }

    private static void AdicionarServicosAplicacao(this IServiceCollection servicos)
    {
        servicos.AddScoped<ISalesApplicationService, SalesApplicationService>();
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

    private static void AdicionarMensageria(this IServiceCollection servicos, IConfiguration configuracao)
    {
        var stringConexao = configuracao["RabbitMq:ConnectionString"];
        var nomeFila = configuracao["RabbitMq:QueueName"];

        if (string.IsNullOrWhiteSpace(stringConexao) || string.IsNullOrWhiteSpace(nomeFila))
        {
            return;
        }

        servicos.AddRebus(
            configurador => configurador.Transport(t => t.UseRabbitMq(stringConexao, nomeFila))
        );
    }
}