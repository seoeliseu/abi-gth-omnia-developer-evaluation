namespace Ambev.DeveloperEvaluation.Application.Common.Idempotencia;

public sealed record IdempotencyEntry<T>(string Fingerprint, T Resultado, DateTimeOffset CriadoEm);