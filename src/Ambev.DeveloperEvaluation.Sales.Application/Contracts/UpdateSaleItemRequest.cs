namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record UpdateSaleItemRequest(long ProductId, int Quantidade);