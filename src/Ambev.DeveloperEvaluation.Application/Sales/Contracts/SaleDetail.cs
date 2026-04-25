namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public sealed record SaleDetail(
    Guid Id,
    string Numero,
    DateTimeOffset DataVenda,
    long ClienteId,
    string ClienteNome,
    long FilialId,
    string FilialNome,
    decimal ValorTotal,
    bool Cancelada,
    IReadOnlyCollection<SaleItemSnapshot> Itens);