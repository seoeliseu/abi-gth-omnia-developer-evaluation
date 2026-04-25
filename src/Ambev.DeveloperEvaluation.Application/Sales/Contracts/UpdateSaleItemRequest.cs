namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public sealed record UpdateSaleItemRequest(long ProductId, int Quantidade);