using Ambev.DeveloperEvaluation.Auth.Application.Contracts;
using Ambev.DeveloperEvaluation.ServiceDefaults.Resultados;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.Auth.WebApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _authService.AutenticarAsync(requisicao, cancellationToken);
        return this.ParaActionResult(resultado, autenticado => Ok(new { token = autenticado.Token }));
    }
}