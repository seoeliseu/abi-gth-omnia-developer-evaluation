namespace Ambev.DeveloperEvaluation.Sales.Application.Common.Idempotencia;

public sealed record IdempotencyEntry<T>(string Fingerprint, T Resultado, DateTimeOffset CriadoEm);