namespace Ambev.DeveloperEvaluation.Common.Security;

public interface IAccessTokenIssuer
{
    AccessToken IssueToken(long usuarioId, string nomeUsuario);
}