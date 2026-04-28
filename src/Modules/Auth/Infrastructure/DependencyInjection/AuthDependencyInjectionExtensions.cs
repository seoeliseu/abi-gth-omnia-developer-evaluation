using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Auth.Application.Services;
using Ambev.DeveloperEvaluation.Auth.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Auth.Infrastructure.DependencyInjection;

public static class AuthDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloAuth(this IServiceCollection servicos)
    {
        servicos.AddScoped<IAuthUserRepository, AuthUserRepositoryEf>();
        servicos.AddScoped<IAuthService, AuthApplicationService>();
        return servicos;
    }
}
