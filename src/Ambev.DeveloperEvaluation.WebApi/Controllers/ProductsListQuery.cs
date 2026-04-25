using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Controllers;

public sealed class ProductsListQuery
{
    [FromQuery(Name = "_page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; init; } = 10;

    [FromQuery(Name = "_order")]
    public string? Order { get; init; }

    [FromQuery(Name = "category")]
    public string? Category { get; init; }

    [FromQuery(Name = "title")]
    public string? Title { get; init; }

    [FromQuery(Name = "_minPrice")]
    public decimal? MinPrice { get; init; }

    [FromQuery(Name = "_maxPrice")]
    public decimal? MaxPrice { get; init; }
}