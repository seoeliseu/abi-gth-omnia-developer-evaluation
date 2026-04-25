namespace Ambev.DeveloperEvaluation.Application.Products.Contracts;

public sealed record ProductDetail(
    long Id,
    string Title,
    decimal Price,
    string Description,
    string Category,
    string Image,
    ProductRatingData Rating,
    bool Active);