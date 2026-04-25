namespace Ambev.DeveloperEvaluation.Application.Common.Idempotencia;

public interface IIdempotencyStore
{
    bool TryGet<T>(string escopo, string chave, out IdempotencyEntry<T>? entrada);
    void Set<T>(string escopo, string chave, string fingerprint, T resultado);
}