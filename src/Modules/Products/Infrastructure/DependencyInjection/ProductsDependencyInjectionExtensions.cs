using System.Linq;
using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.Products.Application.Repositories;
using Ambev.DeveloperEvaluation.Products.Application.Services;
using Ambev.DeveloperEvaluation.Products.Infrastructure.Caching;
using Ambev.DeveloperEvaluation.Products.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Products.Infrastructure.DependencyInjection;

public static class ProductsDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloProducts(this IServiceCollection servicos)
    {
        return servicos.AdicionarModuloProducts(configuracao: null);
    }

    public static IServiceCollection AdicionarModuloProducts(this IServiceCollection servicos, IConfiguration? configuracao)
    {
        servicos.AddScoped<IProductRepository, ProductRepositoryEf>();
        servicos.AddScoped<ProductsApplicationService>();

        var redisConnectionString = configuracao?.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            servicos.AddScoped<IProductsService>(provider => provider.GetRequiredService<ProductsApplicationService>());
            return servicos;
        }

        if (!servicos.Any(descriptor => descriptor.ServiceType == typeof(IDistributedCache)))
        {
            servicos.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        }

        servicos.AddScoped<IProductsService, CachedProductsService>();
        return servicos;
    }
}
