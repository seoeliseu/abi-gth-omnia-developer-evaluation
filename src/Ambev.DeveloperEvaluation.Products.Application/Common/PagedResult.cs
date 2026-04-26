namespace Ambev.DeveloperEvaluation.Products.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Data,
    int TotalItems,
    int CurrentPage,
    int TotalPages);
