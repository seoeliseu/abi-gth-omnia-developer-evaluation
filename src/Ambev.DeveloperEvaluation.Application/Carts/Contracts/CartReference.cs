namespace Ambev.DeveloperEvaluation.Application.Carts.Contracts;

public sealed record CartReference(
    long Id,
    long UsuarioId,
    DateTimeOffset Data,
    IReadOnlyCollection<CartItemReference> Produtos);