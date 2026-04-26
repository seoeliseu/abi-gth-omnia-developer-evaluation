namespace Ambev.DeveloperEvaluation.Carts.Application.Contracts;

public sealed record UpsertCartRequest(
    long UserId,
    DateTimeOffset Date,
    IReadOnlyCollection<CartItemReference> Products);
