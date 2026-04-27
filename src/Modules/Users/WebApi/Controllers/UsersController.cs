using Ambev.DeveloperEvaluation.Users.Application.Contracts;
using Ambev.DeveloperEvaluation.ServiceDefaults.Resultados;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.Users.WebApi.Controllers;

[ApiController]
[Route("api/users")]
public sealed class UsersController : ControllerBase
{
    private readonly IUsersService _usersService;

    public UsersController(IUsersService usersService)
    {
        _usersService = usersService;
    }

    [HttpGet]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarAsync([FromQuery] UsersListQuery consulta, CancellationToken cancellationToken)
    {
        var filtro = new UserListFilter(consulta.Page, consulta.Size, consulta.Order, consulta.Username, consulta.Email, consulta.Role, consulta.Status);
        var resultado = await _usersService.ListarAsync(filtro, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPost]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CriarAsync([FromBody] UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _usersService.CriarAsync(requisicao, cancellationToken);
        return this.ParaActionResult(resultado, usuario => CreatedAtAction(nameof(ObterPorIdAsync), new { id = usuario.Id }, usuario));
    }

    [HttpGet("{id:long}")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _usersService.ObterDetalhePorIdAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPut("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> AtualizarAsync(long id, [FromBody] UpsertUserRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _usersService.AtualizarAsync(id, requisicao, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpDelete("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> RemoverAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _usersService.RemoverAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }
}