namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record CreateSaleItemRequest(long ProductId, int Quantidade);