namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record UpdateSaleRequest(
    DateTimeOffset DataVenda,
    long ClienteId,
    long FilialId,
    string FilialNome,
    IReadOnlyCollection<UpdateSaleItemRequest> Itens);