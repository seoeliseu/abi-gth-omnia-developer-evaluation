namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class SaleItem
{
    public Guid Id { get; }
    public long ProductId { get; }
    public string ProductTitle { get; }
    public int Quantidade { get; private set; }
    public decimal ValorUnitario { get; }
    public decimal PercentualDesconto { get; private set; }
    public bool Cancelado { get; private set; }
    public decimal ValorBruto => Quantidade * ValorUnitario;
    public decimal ValorDesconto => Cancelado ? 0 : decimal.Round(ValorBruto * PercentualDesconto, 2, MidpointRounding.AwayFromZero);
    public decimal ValorTotal => Cancelado ? 0 : ValorBruto - ValorDesconto;

    public SaleItem(long productId, string productTitle, int quantidade, decimal valorUnitario)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId), "O produto deve possuir um identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(productTitle))
        {
            throw new ArgumentException("O título do produto é obrigatório.", nameof(productTitle));
        }

        if (valorUnitario <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(valorUnitario), "O valor unitário deve ser maior que zero.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductTitle = productTitle.Trim();
        ValorUnitario = valorUnitario;

        AtualizarQuantidade(quantidade);
    }

    public void AtualizarQuantidade(int quantidade)
    {
        if (Cancelado)
        {
            throw new InvalidOperationException("Não é possível alterar um item cancelado.");
        }

        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade deve ser maior que zero.");
        }

        if (quantidade > 20)
        {
            throw new InvalidOperationException("Não é possível vender acima de 20 itens idênticos.");
        }

        Quantidade = quantidade;
        PercentualDesconto = CalcularPercentualDesconto(quantidade);
    }

    public void Cancelar()
    {
        Cancelado = true;
    }

    private static decimal CalcularPercentualDesconto(int quantidade)
    {
        if (quantidade >= 10)
        {
            return 0.20m;
        }

        if (quantidade >= 4)
        {
            return 0.10m;
        }

        return 0m;
    }
}