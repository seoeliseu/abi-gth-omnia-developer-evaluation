using Ambev.DeveloperEvaluation.Application.Carts.Contracts;
using Ambev.DeveloperEvaluation.WebApi.Resultados;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.WebApi.Controllers;

[ApiController]
[Route("api/carts")]
public sealed class CartsController : ControllerBase
{
    private readonly ICartsService _cartsService;

    public CartsController(ICartsService cartsService)
    {
        _cartsService = cartsService;
    }

    [HttpGet]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarAsync([FromQuery] CartsListQuery consulta, CancellationToken cancellationToken)
    {
        var filtro = new CartListFilter(consulta.Page, consulta.Size, consulta.Order, consulta.UserId, consulta.MinDate, consulta.MaxDate);
        var resultado = await _cartsService.ListarAsync(filtro, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPost]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CriarAsync([FromBody] UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _cartsService.CriarAsync(requisicao, cancellationToken);
        return this.ParaActionResult(resultado, carrinho => CreatedAtAction(nameof(ObterPorIdAsync), new { id = carrinho.Id }, carrinho));
    }

    [HttpGet("{id:long}")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _cartsService.ObterPorIdAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPut("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> AtualizarAsync(long id, [FromBody] UpsertCartRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _cartsService.AtualizarAsync(id, requisicao, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpDelete("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> RemoverAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _cartsService.RemoverAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, _ => Ok(new { message = "Cart deleted successfully" }));
    }
}