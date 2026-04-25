namespace Ambev.DeveloperEvaluation.Application.Common.Idempotencia;

public sealed record IdempotencyEntry(string Fingerprint, object Resultado, DateTimeOffset CriadoEm);