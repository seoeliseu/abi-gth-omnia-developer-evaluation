namespace Ambev.DeveloperEvaluation.Application.Carts.Contracts;

public sealed record CartListFilter(
    int Page = 1,
    int Size = 10,
    string? Order = null,
    long? UserId = null,
    DateTimeOffset? MinDate = null,
    DateTimeOffset? MaxDate = null);