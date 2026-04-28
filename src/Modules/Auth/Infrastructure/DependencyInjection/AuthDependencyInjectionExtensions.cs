using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Auth.Application.Handlers;
using Ambev.DeveloperEvaluation.Auth.Application.Mappings;
using Ambev.DeveloperEvaluation.Auth.Application.Repositories;
using Ambev.DeveloperEvaluation.Auth.Application.Services;
using Ambev.DeveloperEvaluation.Auth.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Auth.Infrastructure.DependencyInjection;

public static class AuthDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloAuth(this IServiceCollection servicos)
    {
        servicos.AddMediatR(typeof(LoginCommandHandler).Assembly);
        servicos.AddAutoMapper(typeof(AuthMappingProfile).Assembly);
        servicos.AddScoped<IAuthUserRepository, AuthUserRepositoryEf>();
        servicos.AddScoped<IAuthService, AuthApplicationService>();
        return servicos;
    }
}
