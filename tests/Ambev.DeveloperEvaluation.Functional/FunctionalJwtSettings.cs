namespace Ambev.DeveloperEvaluation.Functional;

internal static class FunctionalJwtSettings
{
    public const string Issuer = "developer-evaluation-functional-tests";
    public const string Audience = "developer-evaluation-functional-clients";
    public const string SecretKey = "functional-tests-secret-key-with-32-bytes-min";
    public const string ExpirationMinutes = "60";

    public static IReadOnlyDictionary<string, string?> EnvironmentVariables { get; } = new Dictionary<string, string?>
    {
        ["Jwt__Issuer"] = Issuer,
        ["Jwt__Audience"] = Audience,
        ["Jwt__SecretKey"] = SecretKey,
        ["Jwt__ExpirationMinutes"] = ExpirationMinutes
    };

    public static IReadOnlyDictionary<string, string?> ConfigurationValues { get; } = new Dictionary<string, string?>
    {
        ["Jwt:Issuer"] = Issuer,
        ["Jwt:Audience"] = Audience,
        ["Jwt:SecretKey"] = SecretKey,
        ["Jwt:ExpirationMinutes"] = ExpirationMinutes
    };
}