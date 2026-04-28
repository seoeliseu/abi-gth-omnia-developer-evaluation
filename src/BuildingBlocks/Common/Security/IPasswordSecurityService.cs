namespace Ambev.DeveloperEvaluation.Common.Security;

public interface IPasswordSecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string storedPassword);
    bool NeedsRehash(string storedPassword);
}