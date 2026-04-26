namespace Ambev.DeveloperEvaluation.Products.Application.Contracts;

public sealed record ProductDetail(
    long Id,
    string Title,
    decimal Price,
    string Description,
    string Category,
    string Image,
    ProductRatingData Rating,
    bool Active);
