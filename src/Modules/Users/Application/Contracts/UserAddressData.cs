namespace Ambev.DeveloperEvaluation.Users.Application.Contracts;

public sealed record UserAddressData(
    string City,
    string Street,
    int Number,
    string Zipcode,
    UserGeolocationData Geolocation);
