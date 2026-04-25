namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public sealed record SaleItemSnapshot(
    Guid Id,
    long ProductId,
    string ProductTitle,
    int Quantidade,
    decimal ValorUnitario,
    decimal PercentualDesconto,
    decimal ValorDesconto,
    decimal ValorTotal,
    bool Cancelado);