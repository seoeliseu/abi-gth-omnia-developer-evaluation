namespace Ambev.DeveloperEvaluation.Application.Carts.Contracts;

public sealed record UpsertCartRequest(
    long UserId,
    DateTimeOffset Date,
    IReadOnlyCollection<CartItemReference> Products);