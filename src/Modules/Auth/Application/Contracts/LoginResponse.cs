namespace Ambev.DeveloperEvaluation.Auth.Application.Contracts;

public sealed record LoginResponse(string Token, string TokenType, DateTimeOffset ExpiresAt);