namespace Ambev.DeveloperEvaluation.Carts.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyCollection<T> Data,
    int TotalItems,
    int CurrentPage,
    int TotalPages);
