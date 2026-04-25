using Ambev.DeveloperEvaluation.Application.Common;
using Ambev.DeveloperEvaluation.Common.Results;

namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public interface ISalesApplicationService
{
    Task<Result<SaleDetail>> CriarAsync(CreateSaleRequest requisicao, string? chaveIdempotencia, CancellationToken cancellationToken);
    Task<Result<SaleDetail>> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken);
    Task<Result<PagedResult<SaleSnapshot>>> ListarAsync(SaleListFilter filtro, CancellationToken cancellationToken);
    Task<Result<SaleDetail>> AtualizarAsync(Guid saleId, UpdateSaleRequest requisicao, CancellationToken cancellationToken);
    Task<Result<SaleDetail>> CancelarVendaAsync(Guid saleId, string? chaveIdempotencia, CancellationToken cancellationToken);
    Task<Result<SaleDetail>> CancelarItemAsync(Guid saleId, Guid saleItemId, string? chaveIdempotencia, CancellationToken cancellationToken);
}