namespace Ambev.DeveloperEvaluation.Products.Application.Contracts;

public sealed record ProductListFilter(
    int Page = 1,
    int Size = 10,
    string? Order = null,
    string? Category = null,
    string? Title = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null);
