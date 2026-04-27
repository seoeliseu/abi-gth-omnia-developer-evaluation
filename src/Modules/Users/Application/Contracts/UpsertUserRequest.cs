namespace Ambev.DeveloperEvaluation.Users.Application.Contracts;

public sealed record UpsertUserRequest(
    string Email,
    string Username,
    string Password,
    UserNameData Name,
    UserAddressData Address,
    string Phone,
    string Status,
    string Role);
