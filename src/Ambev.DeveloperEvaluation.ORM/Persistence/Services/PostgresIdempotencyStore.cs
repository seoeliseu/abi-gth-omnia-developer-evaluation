using System.Reflection;
using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Results;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class PostgresIdempotencyStore : IIdempotencyStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

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

        var resultado = DesserializarResultado<T>(entidade.ResultPayload);
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
        entidade.ResultPayload = SerializarResultado(resultado);
        entidade.ResultType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name;
        entidade.CreatedAt = DateTimeOffset.UtcNow;
        _context.SaveChanges();
    }

    private static T? DesserializarResultado<T>(string payload)
    {
        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var envelope = JsonSerializer.Deserialize<StoredResultEnvelope>(payload, SerializerOptions);
            if (envelope is null)
            {
                return default;
            }

            return (T?)CriarResultGenerico(typeof(T), envelope);
        }

        return JsonSerializer.Deserialize<T>(payload, SerializerOptions);
    }

    private static string SerializarResultado<T>(T resultado)
    {
        if (resultado is not null && typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Result<>))
        {
            var tipoResultado = typeof(T);
            var tipoValor = tipoResultado.GetGenericArguments()[0];
            var valor = tipoResultado.GetProperty(nameof(Result<object>.Value))?.GetValue(resultado);
            var isSuccess = (bool)(tipoResultado.GetProperty(nameof(Result.IsSuccess))?.GetValue(resultado) ?? false);
            var errors = ((IReadOnlyList<ResultError>?)tipoResultado.GetProperty(nameof(Result.Errors))?.GetValue(resultado))?.ToArray() ?? [];
            var errorType = (ResultErrorType)(tipoResultado.GetProperty(nameof(Result.ErrorType))?.GetValue(resultado) ?? ResultErrorType.None);
            var statusCode = (int?)tipoResultado.GetProperty(nameof(Result.StatusCode))?.GetValue(resultado);

            var envelope = new StoredResultEnvelope(
                isSuccess,
                valor is null ? null : JsonSerializer.Serialize(valor, tipoValor, SerializerOptions),
                errors,
                errorType,
                statusCode);

            return JsonSerializer.Serialize(envelope, SerializerOptions);
        }

        return JsonSerializer.Serialize(resultado, SerializerOptions);
    }

    private static object CriarResultGenerico(Type tipoResultado, StoredResultEnvelope envelope)
    {
        if (envelope.IsSuccess)
        {
            var tipoValor = tipoResultado.GetGenericArguments()[0];
            var valor = envelope.ValuePayload is null
                ? null
                : JsonSerializer.Deserialize(envelope.ValuePayload, tipoValor, SerializerOptions);

            var metodoSuccess = tipoResultado.GetMethod(nameof(Result<object>.Success), BindingFlags.Public | BindingFlags.Static, [tipoValor]);
            return metodoSuccess?.Invoke(null, [valor])
                ?? throw new InvalidOperationException($"Não foi possível reconstruir o resultado de sucesso para {tipoResultado.Name}.");
        }

        var metodoFailure = tipoResultado.GetMethod(
            nameof(Result<object>.Failure),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(ResultErrorType), typeof(IEnumerable<ResultError>), typeof(int?)],
            modifiers: null);

        return metodoFailure?.Invoke(null, [envelope.ErrorType, envelope.Errors, envelope.StatusCode])
            ?? throw new InvalidOperationException($"Não foi possível reconstruir o resultado de falha para {tipoResultado.Name}.");
    }

    private sealed record StoredResultEnvelope(
        bool IsSuccess,
        string? ValuePayload,
        ResultError[] Errors,
        ResultErrorType ErrorType,
        int? StatusCode);
}