namespace Ambev.DeveloperEvaluation.Carts.Application.Contracts;

public sealed record CartReference(
    long Id,
    long UsuarioId,
    DateTimeOffset Data,
    IReadOnlyCollection<CartItemReference> Produtos);
