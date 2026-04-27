using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Repositories;
using Ambev.DeveloperEvaluation.Products.Application.Services;
using Ambev.DeveloperEvaluation.Products.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Products.Infrastructure.DependencyInjection;

public static class ProductsDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloProducts(this IServiceCollection servicos)
    {
        servicos.AddScoped<IProductRepository, ProductRepositoryEf>();
        servicos.AddScoped<IProductsService, ProductsApplicationService>();
        return servicos;
    }
}
