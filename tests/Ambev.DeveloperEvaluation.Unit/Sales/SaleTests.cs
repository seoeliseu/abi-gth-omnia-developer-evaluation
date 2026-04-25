using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Unit.Sales;

public class SaleTests
{
    [Fact]
    public void Deve_registrar_evento_de_criacao_ao_instanciar_venda()
    {
        var sale = CriarVenda();

        Assert.Contains(sale.DomainEvents, evento => evento is SaleCreatedEvent);
    }

    [Fact]
    public void Deve_cancelar_item_e_zerar_total_do_item()
    {
        var sale = CriarVenda();
        var item = sale.AdicionarItem(10, "Produto", 4, 50m);

        sale.CancelarItem(item.Id);

        Assert.True(item.Cancelado);
        Assert.Equal(0m, item.ValorTotal);
        Assert.Contains(sale.DomainEvents, evento => evento is ItemCancelledEvent);
    }

    [Fact]
    public void Deve_cancelar_venda_e_todos_os_itens_ativos()
    {
        var sale = CriarVenda();
        var itemA = sale.AdicionarItem(10, "Produto A", 4, 50m);
        var itemB = sale.AdicionarItem(20, "Produto B", 2, 30m);

        sale.CancelarVenda();

        Assert.True(sale.Cancelada);
        Assert.True(itemA.Cancelado);
        Assert.True(itemB.Cancelado);
        Assert.Equal(0m, sale.ValorTotal);
        Assert.Contains(sale.DomainEvents, evento => evento is SaleCancelledEvent);
    }

    private static Sale CriarVenda()
    {
        return new Sale(
            numero: "VEN-0001",
            dataVenda: new DateTimeOffset(2026, 4, 25, 12, 0, 0, TimeSpan.Zero),
            clienteId: 100,
            clienteNome: "Cliente Exemplo",
            filialId: 10,
            filialNome: "Filial Centro");
    }
}