namespace Ambev.DeveloperEvaluation.Application.Users.Contracts;

public sealed record UserAddressData(
    string City,
    string Street,
    int Number,
    string Zipcode,
    UserGeolocationData Geolocation);