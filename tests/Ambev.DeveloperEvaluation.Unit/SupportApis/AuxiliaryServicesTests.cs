using Ambev.DeveloperEvaluation.Application.Auth.Contracts;
using Ambev.DeveloperEvaluation.Application.Carts.Contracts;
using Ambev.DeveloperEvaluation.Application.Products.Contracts;
using Ambev.DeveloperEvaluation.Application.Users.Contracts;
using Ambev.DeveloperEvaluation.IoC.Aplicacao;

namespace Ambev.DeveloperEvaluation.Unit.SupportApis;

public class AuxiliaryServicesTests
{
    [Fact]
    public async Task Deve_listar_produtos_com_paginacao()
    {
        var service = new ProductsServiceEmMemoria();

        var resultado = await service.ListarAsync(new ProductListFilter(Page: 1, Size: 2, Order: "price desc"), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.NotNull(resultado.Value);
        Assert.Equal(2, resultado.Value!.Data.Count);
        Assert.True(resultado.Value.TotalItems >= 3);
    }

    [Fact]
    public async Task Deve_criar_e_obter_usuario()
    {
        var service = new UsersServiceEmMemoria();
        var requisicao = new UpsertUserRequest(
            "novo@example.com",
            "novo",
            "123456",
            new UserNameData("Novo", "Usuário"),
            new UserAddressData("São Paulo", "Rua A", 10, "01000-000", new UserGeolocationData("-23.55", "-46.63")),
            "11999999999",
            "Active",
            "Customer");

        var criado = await service.CriarAsync(requisicao, CancellationToken.None);
        var obtido = await service.ObterDetalhePorIdAsync(criado.Value!.Id, CancellationToken.None);

        Assert.True(criado.IsSuccess);
        Assert.True(obtido.IsSuccess);
        Assert.Equal("novo", obtido.Value!.Username);
    }

    [Fact]
    public async Task Deve_autenticar_usuario_valido()
    {
        var service = new AuthServiceEmMemoria();

        var resultado = await service.AutenticarAsync(new LoginRequest("john", "123456"), CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Value?.Token));
    }

    [Fact]
    public async Task Deve_criar_carrinho()
    {
        var service = new CartsServiceEmMemoria();
        var requisicao = new UpsertCartRequest(3, DateTimeOffset.UtcNow, [new CartItemReference(10, 2)]);

        var resultado = await service.CriarAsync(requisicao, CancellationToken.None);

        Assert.True(resultado.IsSuccess);
        Assert.Equal(3, resultado.Value?.UsuarioId);
    }
}