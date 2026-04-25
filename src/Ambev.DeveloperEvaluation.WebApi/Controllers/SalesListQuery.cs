using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.WebApi.Controllers;

public sealed class SalesListQuery
{
    [FromQuery(Name = "_page")]
    public int Page { get; init; } = 1;

    [FromQuery(Name = "_size")]
    public int Size { get; init; } = 10;

    [FromQuery(Name = "_order")]
    public string? Order { get; init; }

    [FromQuery(Name = "numero")]
    public string? Numero { get; init; }

    [FromQuery(Name = "clienteNome")]
    public string? ClienteNome { get; init; }

    [FromQuery(Name = "filialNome")]
    public string? FilialNome { get; init; }

    [FromQuery(Name = "cancelada")]
    public bool? Cancelada { get; init; }

    [FromQuery(Name = "_minDataVenda")]
    public DateTimeOffset? DataMinima { get; init; }

    [FromQuery(Name = "_maxDataVenda")]
    public DateTimeOffset? DataMaxima { get; init; }
}