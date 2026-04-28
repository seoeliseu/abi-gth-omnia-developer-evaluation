using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ambev.DeveloperEvaluation.Functional.Users;

public sealed class UsersApiFunctionalTests : IClassFixture<UsersApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public UsersApiFunctionalTests(UsersApiFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task GetUsers_DeveListarUsuariosSeedados()
    {
        using var resposta = await _client.GetAsync("/api/users?page=1&size=10");
        var usuarios = await resposta.Content.ReadFromJsonAsync<PagedResponse<UserResponse>>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.NotNull(usuarios);
        Assert.True(usuarios!.TotalItems >= 2);
        Assert.Contains(usuarios.Data, usuario => usuario.Username == "john" && usuario.Email == "john@example.com");
    }

    [Fact]
    public async Task PostUsers_DeveCriarUsuarioEPermitirConsulta()
    {
        var requisicao = CriarRequisicaoUsuario(
            "maria.silva@example.com",
            "mariasilva",
            "Maria",
            "Silva",
            "Campinas",
            "Rua das Flores",
            120,
            "13010-000",
            "-22.9056",
            "-47.0608",
            "19999990000",
            "Active",
            "Customer");

        using var criarResposta = await _client.PostAsJsonAsync("/api/users", requisicao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);
        Assert.Equal(requisicao.Email, criado!.Email);
        Assert.Equal(requisicao.Username, criado.Username);
        Assert.Equal(requisicao.Name.Firstname, criado.Name.Firstname);

        using var obterResposta = await _client.GetAsync($"/api/users/{criado.Id}");
        var obtido = await obterResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, obterResposta.StatusCode);
        Assert.NotNull(obtido);
        Assert.Equal(criado.Id, obtido!.Id);
        Assert.Equal(requisicao.Email, obtido.Email);
        Assert.Equal(requisicao.Address.Geolocation.Long, obtido.Address.Geolocation.Long);
    }

    [Fact]
    public async Task PutUsers_DeveAtualizarUsuarioExistente()
    {
        var requisicaoCriacao = CriarRequisicaoUsuario(
            "ana.souza@example.com",
            "anasouza",
            "Ana",
            "Souza",
            "Santos",
            "Rua Alfa",
            40,
            "11000-000",
            "-23.9608",
            "-46.3336",
            "13999990000",
            "Active",
            "Customer");

        using var criarResposta = await _client.PostAsJsonAsync("/api/users", requisicaoCriacao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);

        var requisicaoAtualizacao = CriarRequisicaoUsuario(
            "ana.souza@example.com",
            "ana.souza.atualizada",
            "Ana Paula",
            "Souza",
            "São Vicente",
            "Avenida Beta",
            85,
            "11300-000",
            "-23.9631",
            "-46.3919",
            "13999991111",
            "Inactive",
            "Manager");

        using var atualizarResposta = await _client.PutAsJsonAsync($"/api/users/{criado!.Id}", requisicaoAtualizacao);
        var atualizado = await atualizarResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, atualizarResposta.StatusCode);
        Assert.NotNull(atualizado);
        Assert.Equal(requisicaoAtualizacao.Username, atualizado!.Username);
        Assert.Equal(requisicaoAtualizacao.Name.Firstname, atualizado.Name.Firstname);
        Assert.Equal(requisicaoAtualizacao.Status, atualizado.Status);
        Assert.Equal(requisicaoAtualizacao.Role, atualizado.Role);

        using var obterResposta = await _client.GetAsync($"/api/users/{criado.Id}");
        var obtido = await obterResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, obterResposta.StatusCode);
        Assert.NotNull(obtido);
        Assert.Equal(requisicaoAtualizacao.Address.City, obtido!.Address.City);
        Assert.Equal(requisicaoAtualizacao.Phone, obtido.Phone);
    }

    [Fact]
    public async Task DeleteUsers_DeveRemoverUsuarioECausarNotFoundNaConsulta()
    {
        var requisicao = CriarRequisicaoUsuario(
            "carlos.lima@example.com",
            "carloslima",
            "Carlos",
            "Lima",
            "Rio de Janeiro",
            "Rua Gama",
            300,
            "20000-000",
            "-22.9068",
            "-43.1729",
            "21999990000",
            "Active",
            "Customer");

        using var criarResposta = await _client.PostAsJsonAsync("/api/users", requisicao);
        var criado = await criarResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, criarResposta.StatusCode);
        Assert.NotNull(criado);

        using var removerResposta = await _client.DeleteAsync($"/api/users/{criado!.Id}");
        var removido = await removerResposta.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, removerResposta.StatusCode);
        Assert.NotNull(removido);
        Assert.Equal(criado.Id, removido!.Id);

        using var obterResposta = await _client.GetAsync($"/api/users/{criado.Id}");

        Assert.Equal(HttpStatusCode.NotFound, obterResposta.StatusCode);
    }

    private static UserRequest CriarRequisicaoUsuario(
        string email,
        string username,
        string firstname,
        string lastname,
        string city,
        string street,
        int number,
        string zipcode,
        string lat,
        string longitude,
        string phone,
        string status,
        string role)
    {
        return new UserRequest(
            email,
            username,
            "Senha@123",
            new UserRequestName(firstname, lastname),
            new UserRequestAddress(
                city,
                street,
                number,
                zipcode,
                new UserRequestGeolocation(lat, longitude)),
            phone,
            status,
            role);
    }

    private sealed record PagedResponse<T>(IReadOnlyCollection<T> Data, int TotalItems, int CurrentPage, int TotalPages);

    private sealed record UserResponse(
        long Id,
        string Email,
        string Username,
        string Password,
        UserNameResponse Name,
        UserAddressResponse Address,
        string Phone,
        string Status,
        string Role);

    private sealed record UserNameResponse(string Firstname, string Lastname);

    private sealed record UserAddressResponse(string City, string Street, int Number, string Zipcode, UserGeolocationResponse Geolocation);

    private sealed record UserGeolocationResponse(string Lat, string Long);

    private sealed record UserRequest(
        string Email,
        string Username,
        string Password,
        UserRequestName Name,
        UserRequestAddress Address,
        string Phone,
        string Status,
        string Role);

    private sealed record UserRequestName(string Firstname, string Lastname);

    private sealed record UserRequestAddress(string City, string Street, int Number, string Zipcode, UserRequestGeolocation Geolocation);

    private sealed record UserRequestGeolocation(string Lat, string Long);
}