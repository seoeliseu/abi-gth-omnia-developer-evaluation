namespace Ambev.DeveloperEvaluation.Application.Common.Idempotencia;

public interface IIdempotencyStore
{
    bool TryGet(string escopo, string chave, out IdempotencyEntry? entrada);
    void Set(string escopo, string chave, string fingerprint, object resultado);
}