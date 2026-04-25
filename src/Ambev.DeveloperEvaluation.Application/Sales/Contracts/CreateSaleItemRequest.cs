namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public sealed record CreateSaleItemRequest(long ProductId, int Quantidade);