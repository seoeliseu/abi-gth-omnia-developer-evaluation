namespace Ambev.DeveloperEvaluation.Application.Sales.Contracts;

public sealed record UpdateSaleRequest(
    DateTimeOffset DataVenda,
    long ClienteId,
    long FilialId,
    string FilialNome,
    IReadOnlyCollection<UpdateSaleItemRequest> Itens);