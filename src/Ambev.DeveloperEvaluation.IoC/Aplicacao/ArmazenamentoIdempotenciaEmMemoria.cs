using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class ArmazenamentoIdempotenciaEmMemoria : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, IdempotencyEntry> _entradas = new();

    public bool TryGet(string escopo, string chave, out IdempotencyEntry? entrada)
    {
        if (_entradas.TryGetValue(ComporChave(escopo, chave), out var valor))
        {
            entrada = valor;
            return true;
        }

        entrada = null;
        return false;
    }

    public void Set(string escopo, string chave, string fingerprint, object resultado)
    {
        _entradas[ComporChave(escopo, chave)] = new IdempotencyEntry(fingerprint, resultado, DateTimeOffset.UtcNow);
    }

    private static string ComporChave(string escopo, string chave) => $"{escopo}:{chave}";
}