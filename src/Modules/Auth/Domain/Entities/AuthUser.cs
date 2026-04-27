namespace Ambev.DeveloperEvaluation.Auth.Domain.Entities;

public sealed class AuthUser
{
    private AuthUser(long usuarioId, string nomeUsuario, string senha)
    {
        UsuarioId = usuarioId;
        NomeUsuario = nomeUsuario;
        Senha = senha;
    }

    public long UsuarioId { get; }
    public string NomeUsuario { get; }
    public string Senha { get; }

    public static AuthUser Reidratar(long usuarioId, string nomeUsuario, string senha)
        => new(usuarioId, nomeUsuario, senha);
}
