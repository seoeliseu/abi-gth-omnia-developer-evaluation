namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record SaleSnapshot(
    Guid Id,
    string Numero,
    DateTimeOffset DataVenda,
    long ClienteId,
    string ClienteNome,
    long FilialId,
    string FilialNome,
    decimal ValorTotal,
    bool Cancelada);