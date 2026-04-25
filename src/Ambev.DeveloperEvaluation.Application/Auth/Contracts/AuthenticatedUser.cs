namespace Ambev.DeveloperEvaluation.Application.Auth.Contracts;

public sealed record AuthenticatedUser(
    long UsuarioId,
    string NomeUsuario,
    string Token,
    DateTimeOffset ExpiraEm);