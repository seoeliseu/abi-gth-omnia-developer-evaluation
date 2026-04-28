using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Common.Results;
using MediatR;

namespace Ambev.DeveloperEvaluation.Auth.Application.Handlers;

public sealed record LoginCommand(LoginRequest Requisicao) : IRequest<Result<AuthenticatedUser>>;