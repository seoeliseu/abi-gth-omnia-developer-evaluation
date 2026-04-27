using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Ambev.DeveloperEvaluation.Users.Application.Repositories;
using Ambev.DeveloperEvaluation.Users.Application.Services;
using Ambev.DeveloperEvaluation.Users.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Ambev.DeveloperEvaluation.Users.Infrastructure.DependencyInjection;

public static class UsersDependencyInjectionExtensions
{
    public static IServiceCollection AdicionarModuloUsers(this IServiceCollection servicos)
    {
        servicos.AddScoped<IUserRepository, UserRepositoryEf>();
        servicos.AddScoped<IUsersService, UsersApplicationService>();
        return servicos;
    }
}
