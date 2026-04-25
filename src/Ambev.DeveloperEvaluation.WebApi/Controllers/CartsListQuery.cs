using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Controllers;

public sealed class CartsListQuery
{
    [FromQuery(Name = "_page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; init; } = 10;

    [FromQuery(Name = "_order")]
    public string? Order { get; init; }

    [FromQuery(Name = "userId")]
    public long? UserId { get; init; }

    [FromQuery(Name = "_minDate")]
    public DateTimeOffset? MinDate { get; init; }

    [FromQuery(Name = "_maxDate")]
    public DateTimeOffset? MaxDate { get; init; }
}