namespace Ambev.DeveloperEvaluation.Auth.Application.Contracts;

public sealed record AuthenticatedUser(
    long UsuarioId,
    string NomeUsuario,
    string Token,
    DateTimeOffset ExpiraEm);
