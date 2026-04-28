using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Auth.Application.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthenticatedUser>>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<Result<AuthenticatedUser>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return _authService.AutenticarAsync(request.Requisicao, cancellationToken);
    }
}