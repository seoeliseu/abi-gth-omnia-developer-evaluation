using Ambev.DeveloperEvaluation.Application.Sales.Contracts;
using Ambev.DeveloperEvaluation.WebApi.Resultados;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.WebApi.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController : ControllerBase
{
    private readonly ISalesApplicationService _salesApplicationService;

    public SalesController(ISalesApplicationService salesApplicationService)
    {
        _salesApplicationService = salesApplicationService;
    }

    [HttpPost]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CriarAsync([FromBody] CreateSaleRequest requisicao, CancellationToken cancellationToken)
    {
        var chaveIdempotencia = Request.Headers["Idempotency-Key"].FirstOrDefault();
        PropagarChaveIdempotencia(chaveIdempotencia);

        var resultado = await _salesApplicationService.CriarAsync(requisicao, chaveIdempotencia, cancellationToken);
        return this.ParaActionResult(resultado, detalhe => CreatedAtAction(nameof(ObterPorIdAsync), new { saleId = detalhe.Id }, detalhe));
    }

    [HttpGet("{saleId:guid}")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var resultado = await _salesApplicationService.ObterPorIdAsync(saleId, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpGet]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarAsync([FromQuery] SalesListQuery consulta, CancellationToken cancellationToken)
    {
        var filtro = new SaleListFilter(
            consulta.Page,
            consulta.Size,
            consulta.Order,
            consulta.Numero,
            consulta.ClienteNome,
            consulta.FilialNome,
            consulta.Cancelada,
            consulta.DataMinima,
            consulta.DataMaxima);

        var resultado = await _salesApplicationService.ListarAsync(filtro, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPut("{saleId:guid}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> AtualizarAsync(Guid saleId, [FromBody] UpdateSaleRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _salesApplicationService.AtualizarAsync(saleId, requisicao, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPost("{saleId:guid}/cancel")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CancelarVendaAsync(Guid saleId, CancellationToken cancellationToken)
    {
        var chaveIdempotencia = Request.Headers["Idempotency-Key"].FirstOrDefault();
        PropagarChaveIdempotencia(chaveIdempotencia);

        var resultado = await _salesApplicationService.CancelarVendaAsync(saleId, chaveIdempotencia, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPost("{saleId:guid}/items/{saleItemId:guid}/cancel")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CancelarItemAsync(Guid saleId, Guid saleItemId, CancellationToken cancellationToken)
    {
        var chaveIdempotencia = Request.Headers["Idempotency-Key"].FirstOrDefault();
        PropagarChaveIdempotencia(chaveIdempotencia);

        var resultado = await _salesApplicationService.CancelarItemAsync(saleId, saleItemId, chaveIdempotencia, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    private void PropagarChaveIdempotencia(string? chaveIdempotencia)
    {
        if (!string.IsNullOrWhiteSpace(chaveIdempotencia))
        {
            Response.Headers["Idempotency-Key"] = chaveIdempotencia;
        }
    }
}