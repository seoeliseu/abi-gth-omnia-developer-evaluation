namespace Ambev.DeveloperEvaluation.Application.Users.Contracts;

public sealed record UserReference(
    long Id,
    string NomeUsuario,
    string Email,
    string Status,
    string Papel,
    bool Ativo);