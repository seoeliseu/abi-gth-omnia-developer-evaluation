using Ambev.DeveloperEvaluation.ORM.Persistence.Services;
using Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.Sales.Application.Contracts;
using Ambev.DeveloperEvaluation.Sales.Application.Repositories;
using Ambev.DeveloperEvaluation.Sales.Application.Services;
using Ambev.DeveloperEvaluation.Sales.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Sales.Infrastructure.DependencyInjection;

public static class SalesDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloSales(this IServiceCollection servicos)
    {
        servicos.AddScoped<ISaleRepository, SaleRepositoryEf>();
        servicos.AddScoped<IIdempotencyStore, PostgresIdempotencyStore>();
        servicos.AddScoped<ISalesApplicationService, SalesApplicationService>();
        return servicos;
    }
}