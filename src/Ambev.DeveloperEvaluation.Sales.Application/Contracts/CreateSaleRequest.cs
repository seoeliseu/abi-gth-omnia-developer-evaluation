namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record CreateSaleRequest(
    string Numero,
    DateTimeOffset DataVenda,
    long ClienteId,
    long FilialId,
    string FilialNome,
    IReadOnlyCollection<CreateSaleItemRequest> Itens);