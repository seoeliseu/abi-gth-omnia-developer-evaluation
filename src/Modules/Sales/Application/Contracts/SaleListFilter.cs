namespace Ambev.DeveloperEvaluation.Sales.Application.Contracts;

public sealed record SaleListFilter(
    int Page = 1,
    int Size = 10,
    string? Order = null,
    string? Numero = null,
    string? ClienteNome = null,
    string? FilialNome = null,
    bool? Cancelada = null,
    DateTimeOffset? DataMinima = null,
    DateTimeOffset? DataMaxima = null);