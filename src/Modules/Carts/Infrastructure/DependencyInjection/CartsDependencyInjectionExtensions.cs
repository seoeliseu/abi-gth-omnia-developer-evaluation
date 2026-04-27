using Ambev.DeveloperEvaluation.Carts.Application.Contracts;
using Ambev.DeveloperEvaluation.Carts.Application.Repositories;
using Ambev.DeveloperEvaluation.Carts.Application.Services;
using Ambev.DeveloperEvaluation.Carts.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Carts.Infrastructure.DependencyInjection;

public static class CartsDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloCarts(this IServiceCollection servicos)
    {
        servicos.AddScoped<ICartRepository, CartRepositoryEf>();
        servicos.AddScoped<ICartsService, CartsApplicationService>();
        return servicos;
    }
}
