using System.Security.Cryptography;

namespace Ambev.DeveloperEvaluation.Common.Security;

public sealed class PasswordSecurityService : IPasswordSecurityService
{
    private const string Prefix = "pbkdf2-sha256";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return string.Join(
            '$',
            Prefix,
            Iterations.ToString(),
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool VerifyPassword(string password, string storedPassword)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(storedPassword))
        {
            return false;
        }

        if (!TryParseHash(storedPassword, out var iterations, out var salt, out var expectedHash))
        {
            return string.Equals(password, storedPassword, StringComparison.Ordinal);
        }

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public bool NeedsRehash(string storedPassword)
    {
        return !TryParseHash(storedPassword, out _, out _, out _);
    }

    private static bool TryParseHash(string storedPassword, out int iterations, out byte[] salt, out byte[] hash)
    {
        iterations = 0;
        salt = Array.Empty<byte>();
        hash = Array.Empty<byte>();

        var parts = storedPassword.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 4 || !string.Equals(parts[0], Prefix, StringComparison.Ordinal) || !int.TryParse(parts[1], out iterations))
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            hash = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            iterations = 0;
            salt = Array.Empty<byte>();
            hash = Array.Empty<byte>();
            return false;
        }

        return salt.Length == SaltSize && hash.Length == KeySize;
    }
}