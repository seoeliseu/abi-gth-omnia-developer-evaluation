namespace Ambev.DeveloperEvaluation.Application.Users.Contracts;

public sealed record UserListFilter(
    int Page = 1,
    int Size = 10,
    string? Order = null,
    string? Username = null,
    string? Email = null,
    string? Role = null,
    string? Status = null);