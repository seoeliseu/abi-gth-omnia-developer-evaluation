namespace Ambev.DeveloperEvaluation.Users.Domain.ValueObjects;

public sealed record UserAddress(
    string City,
    string Street,
    int Number,
    string Zipcode,
    UserGeolocation Geolocation);
