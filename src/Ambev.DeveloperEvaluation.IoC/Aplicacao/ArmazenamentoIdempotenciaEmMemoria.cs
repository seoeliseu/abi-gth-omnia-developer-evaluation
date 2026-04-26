using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class ArmazenamentoIdempotenciaEmMemoria : IIdempotencyStore
{
    private readonly ConcurrentDictionary<string, RegistroIdempotencia> _entradas = new();

    public bool TryGet<T>(string escopo, string chave, out IdempotencyEntry<T>? entrada)
    {
        if (_entradas.TryGetValue(ComporChave(escopo, chave), out var valor))
        {
            if (valor.Resultado is T resultadoTipado)
            {
                entrada = new IdempotencyEntry<T>(valor.Fingerprint, resultadoTipado, valor.CriadoEm);
                return true;
            }
        }

        entrada = null;
        return false;
    }

    public void Set<T>(string escopo, string chave, string fingerprint, T resultado)
    {
        _entradas[ComporChave(escopo, chave)] = new RegistroIdempotencia(fingerprint, resultado, DateTimeOffset.UtcNow);
    }

    private static string ComporChave(string escopo, string chave) => $"{escopo}:{chave}";

    private sealed record RegistroIdempotencia(string Fingerprint, object? Resultado, DateTimeOffset CriadoEm);
}