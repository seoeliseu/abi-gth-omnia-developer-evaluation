using Ambev.DeveloperEvaluation.Users.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Users.Domain.Entities;

public sealed class User
{
    private User(
        long id,
        string email,
        string username,
        string password,
        UserName name,
        UserAddress address,
        string phone,
        string status,
        string role)
    {
        Id = id;
        Email = email;
        Username = username;
        Password = password;
        Name = name;
        Address = address;
        Phone = phone;
        Status = status;
        Role = role;
    }

    public long Id { get; private set; }
    public string Email { get; private set; }
    public string Username { get; private set; }
    public string Password { get; private set; }
    public UserName Name { get; private set; }
    public UserAddress Address { get; private set; }
    public string Phone { get; private set; }
    public string Status { get; private set; }
    public string Role { get; private set; }

    public static User Criar(
        string email,
        string username,
        string password,
        UserName name,
        UserAddress address,
        string phone,
        string status,
        string role)
    {
        return new User(0, email, username, password, name, address, phone, status, role);
    }

    public static User Reidratar(
        long id,
        string email,
        string username,
        string password,
        UserName name,
        UserAddress address,
        string phone,
        string status,
        string role)
    {
        return new User(id, email, username, password, name, address, phone, status, role);
    }

    public void Atualizar(
        string email,
        string username,
        string password,
        UserName name,
        UserAddress address,
        string phone,
        string status,
        string role)
    {
        Email = email;
        Username = username;
        Password = password;
        Name = name;
        Address = address;
        Phone = phone;
        Status = status;
        Role = role;
    }
}
