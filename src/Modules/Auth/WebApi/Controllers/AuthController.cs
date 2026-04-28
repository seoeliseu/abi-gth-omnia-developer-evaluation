using AutoMapper;
using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.Auth.Application.Handlers;
using Ambev.DeveloperEvaluation.ServiceDefaults.Resultados;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.Auth.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly ISender _sender;

    public AuthController(IMapper mapper, ISender sender)
    {
        _mapper = mapper;
        _sender = sender;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _sender.Send(new LoginCommand(requisicao), cancellationToken);
        return this.ParaActionResult(resultado, autenticado => Ok(_mapper.Map<LoginResponse>(autenticado)));
    }
}