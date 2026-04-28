using AutoMapper;
using Ambev.DeveloperEvaluation.Auth.Application.Contracts;

namespace Ambev.DeveloperEvaluation.Auth.Application.Mappings;

public sealed class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<AuthenticatedUser, LoginResponse>()
            .ForCtorParam(nameof(LoginResponse.Token), configuracao => configuracao.MapFrom(origem => origem.Token))
            .ForCtorParam(nameof(LoginResponse.TokenType), configuracao => configuracao.MapFrom(_ => "Bearer"))
            .ForCtorParam(nameof(LoginResponse.ExpiresAt), configuracao => configuracao.MapFrom(origem => origem.ExpiraEm));
    }
}