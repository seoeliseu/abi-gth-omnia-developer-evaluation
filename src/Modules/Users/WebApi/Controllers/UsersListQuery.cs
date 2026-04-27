using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.Users.WebApi.Controllers;

public sealed class UsersListQuery
{
    [FromQuery(Name = "_page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; init; } = 10;

    [FromQuery(Name = "_order")]
    public string? Order { get; init; }

    [FromQuery(Name = "username")]
    public string? Username { get; init; }

    [FromQuery(Name = "email")]
    public string? Email { get; init; }

    [FromQuery(Name = "role")]
    public string? Role { get; init; }

    [FromQuery(Name = "status")]
    public string? Status { get; init; }
}