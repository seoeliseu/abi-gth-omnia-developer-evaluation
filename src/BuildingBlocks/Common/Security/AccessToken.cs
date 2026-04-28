namespace Ambev.DeveloperEvaluation.Common.Security;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);