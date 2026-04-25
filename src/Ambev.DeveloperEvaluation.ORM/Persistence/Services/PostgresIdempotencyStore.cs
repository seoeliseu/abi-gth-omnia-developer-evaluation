using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Common.Idempotencia;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class PostgresIdempotencyStore : IIdempotencyStore
{
    private readonly DeveloperEvaluationDbContext _context;

    public PostgresIdempotencyStore(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public bool TryGet<T>(string escopo, string chave, out IdempotencyEntry<T>? entrada)
    {
        var entidade = _context.IdempotencyEntries.SingleOrDefault(item => item.Scope == escopo && item.Key == chave);
        if (entidade is null)
        {
            entrada = null;
            return false;
        }

        var resultado = JsonSerializer.Deserialize<T>(entidade.ResultPayload);
        if (resultado is null)
        {
            entrada = null;
            return false;
        }

        entrada = new IdempotencyEntry<T>(entidade.Fingerprint, resultado, entidade.CreatedAt);
        return true;
    }

    public void Set<T>(string escopo, string chave, string fingerprint, T resultado)
    {
        var entidade = _context.IdempotencyEntries.SingleOrDefault(item => item.Scope == escopo && item.Key == chave);
        if (entidade is null)
        {
            entidade = new IdempotencyEntryEntity
            {
                Scope = escopo,
                Key = chave
            };
            _context.IdempotencyEntries.Add(entidade);
        }

        entidade.Fingerprint = fingerprint;
        entidade.ResultPayload = JsonSerializer.Serialize(resultado);
        entidade.ResultType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name;
        entidade.CreatedAt = DateTimeOffset.UtcNow;
        _context.SaveChanges();
    }
}