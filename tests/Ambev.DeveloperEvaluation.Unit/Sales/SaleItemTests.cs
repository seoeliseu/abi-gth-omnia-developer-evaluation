using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Unit.Sales;

public class SaleItemTests
{
    [Theory]
    [InlineData(3, 0.00, 300.00)]
    [InlineData(4, 0.10, 360.00)]
    [InlineData(10, 0.20, 800.00)]
    public void Deve_aplicar_desconto_conforme_faixa(int quantidade, decimal percentualEsperado, decimal totalEsperado)
    {
        var item = new SaleItem(10, "Produto", quantidade, 100m);

        Assert.Equal(percentualEsperado, item.PercentualDesconto);
        Assert.Equal(totalEsperado, item.ValorTotal);
    }

    [Fact]
    public void Deve_impedir_venda_com_mais_de_vinte_itens_identicos()
    {
        var excecao = Assert.Throws<InvalidOperationException>(() => new SaleItem(10, "Produto", 21, 10m));

        Assert.Equal("Não é possível vender acima de 20 itens idênticos.", excecao.Message);
    }
}