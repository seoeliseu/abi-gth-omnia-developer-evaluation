namespace Ambev.DeveloperEvaluation.Users.Application.Contracts;

public sealed record UserDetail(
    long Id,
    string Email,
    string Username,
    string Password,
    UserNameData Name,
    UserAddressData Address,
    string Phone,
    string Status,
    string Role);
