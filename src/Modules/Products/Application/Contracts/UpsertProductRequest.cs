namespace Ambev.DeveloperEvaluation.Products.Application.Contracts;

public sealed record UpsertProductRequest(
    string Title,
    decimal Price,
    string Description,
    string Category,
    string Image,
    ProductRatingData Rating);
