using Ambev.DeveloperEvaluation.Products.Application.Contracts;
using Ambev.DeveloperEvaluation.ServiceDefaults.Resultados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ambev.DeveloperEvaluation.Products.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductsService _productsService;

    public ProductsController(IProductsService productsService)
    {
        _productsService = productsService;
    }

    [HttpGet]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarAsync([FromQuery] ProductsListQuery consulta, CancellationToken cancellationToken)
    {
        var filtro = new ProductListFilter(consulta.Page, consulta.Size, consulta.Order, consulta.Category, consulta.Title, consulta.MinPrice, consulta.MaxPrice);
        var resultado = await _productsService.ListarAsync(filtro, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPost]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> CriarAsync([FromBody] UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _productsService.CriarAsync(requisicao, cancellationToken);
        return this.ParaActionResult(resultado, produto => CreatedAtAction(nameof(ObterPorIdAsync), new { id = produto.Id }, produto));
    }

    [HttpGet("{id:long}")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ObterPorIdAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _productsService.ObterDetalhePorIdAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpPut("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> AtualizarAsync(long id, [FromBody] UpsertProductRequest requisicao, CancellationToken cancellationToken)
    {
        var resultado = await _productsService.AtualizarAsync(id, requisicao, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpDelete("{id:long}")]
    [EnableRateLimiting("sales-comandos")]
    [RequestTimeout("sales-comandos")]
    public async Task<IActionResult> RemoverAsync(long id, CancellationToken cancellationToken)
    {
        var resultado = await _productsService.RemoverAsync(id, cancellationToken);
        return this.ParaActionResult(resultado, _ => Ok(new { message = "Product deleted successfully" }));
    }

    [HttpGet("categories")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarCategoriasAsync(CancellationToken cancellationToken)
    {
        var resultado = await _productsService.ListarCategoriasAsync(cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }

    [HttpGet("category/{category}")]
    [EnableRateLimiting("sales-consultas")]
    [RequestTimeout("sales-consultas")]
    public async Task<IActionResult> ListarPorCategoriaAsync(string category, [FromQuery] ProductsListQuery consulta, CancellationToken cancellationToken)
    {
        var filtro = new ProductListFilter(consulta.Page, consulta.Size, consulta.Order, category, consulta.Title, consulta.MinPrice, consulta.MaxPrice);
        var resultado = await _productsService.ListarPorCategoriaAsync(category, filtro, cancellationToken);
        return this.ParaActionResult(resultado, Ok);
    }
}