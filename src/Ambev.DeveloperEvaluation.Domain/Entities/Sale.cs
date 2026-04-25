using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

public class Sale
{
    private readonly List<SaleItem> _items = [];
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; }
    public string Numero { get; }
    public DateTimeOffset DataVenda { get; private set; }
    public long ClienteId { get; private set; }
    public string ClienteNome { get; private set; }
    public long FilialId { get; private set; }
    public string FilialNome { get; private set; }
    public bool Cancelada { get; private set; }
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    public decimal ValorTotal => _items.Sum(item => item.ValorTotal);

    public Sale(
        string numero,
        DateTimeOffset dataVenda,
        long clienteId,
        string clienteNome,
        long filialId,
        string filialNome)
    {
        if (string.IsNullOrWhiteSpace(numero))
        {
            throw new ArgumentException("O número da venda é obrigatório.", nameof(numero));
        }

        if (clienteId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clienteId), "O cliente deve possuir um identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(clienteNome))
        {
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(clienteNome));
        }

        if (filialId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(filialId), "A filial deve possuir um identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(filialNome))
        {
            throw new ArgumentException("O nome da filial é obrigatório.", nameof(filialNome));
        }

        Id = Guid.NewGuid();
        Numero = numero.Trim();
        DataVenda = dataVenda;
        ClienteId = clienteId;
        ClienteNome = clienteNome.Trim();
        FilialId = filialId;
        FilialNome = filialNome.Trim();

        RegistrarEvento(new SaleCreatedEvent(Id, Numero));
    }

    public SaleItem AdicionarItem(long productId, string productTitle, int quantidade, decimal valorUnitario)
    {
        GarantirVendaAtiva();

        if (_items.Any(item => item.ProductId == productId && !item.Cancelado))
        {
            throw new InvalidOperationException("Não é permitido adicionar o mesmo produto mais de uma vez na venda.");
        }

        var item = new SaleItem(productId, productTitle, quantidade, valorUnitario);
        _items.Add(item);
        RegistrarEvento(new SaleModifiedEvent(Id, Numero));

        return item;
    }

    public void AtualizarCabecalho(
        DateTimeOffset dataVenda,
        long clienteId,
        string clienteNome,
        long filialId,
        string filialNome)
    {
        GarantirVendaAtiva();

        if (clienteId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(clienteId), "O cliente deve possuir um identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(clienteNome))
        {
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(clienteNome));
        }

        if (filialId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(filialId), "A filial deve possuir um identificador válido.");
        }

        if (string.IsNullOrWhiteSpace(filialNome))
        {
            throw new ArgumentException("O nome da filial é obrigatório.", nameof(filialNome));
        }

        DataVenda = dataVenda;
        ClienteId = clienteId;
        ClienteNome = clienteNome.Trim();
        FilialId = filialId;
        FilialNome = filialNome.Trim();
        RegistrarEvento(new SaleModifiedEvent(Id, Numero));
    }

    public SaleItem AdicionarOuAtualizarItem(long productId, string productTitle, int quantidade, decimal valorUnitario)
    {
        GarantirVendaAtiva();

        var itemExistente = _items.FirstOrDefault(item => item.ProductId == productId && !item.Cancelado);
        if (itemExistente is null)
        {
            return AdicionarItem(productId, productTitle, quantidade, valorUnitario);
        }

        if (itemExistente.ProductTitle != productTitle || itemExistente.ValorUnitario != valorUnitario)
        {
            itemExistente.Cancelar();
            RegistrarEvento(new ItemCancelledEvent(Id, itemExistente.Id, itemExistente.ProductId));
            return AdicionarItem(productId, productTitle, quantidade, valorUnitario);
        }

        itemExistente.AtualizarQuantidade(quantidade);
        RegistrarEvento(new SaleModifiedEvent(Id, Numero));
        return itemExistente;
    }

    public void CancelarItem(Guid saleItemId)
    {
        GarantirVendaAtiva();

        var item = _items.FirstOrDefault(itemAtual => itemAtual.Id == saleItemId)
            ?? throw new InvalidOperationException("O item informado não pertence à venda.");

        if (item.Cancelado)
        {
            return;
        }

        item.Cancelar();
        RegistrarEvento(new ItemCancelledEvent(Id, item.Id, item.ProductId));
        RegistrarEvento(new SaleModifiedEvent(Id, Numero));
    }

    public void CancelarVenda()
    {
        if (Cancelada)
        {
            return;
        }

        foreach (var item in _items.Where(itemAtual => !itemAtual.Cancelado))
        {
            item.Cancelar();
        }

        Cancelada = true;
        RegistrarEvento(new SaleCancelledEvent(Id, Numero));
    }

    public void LimparEventos()
    {
        _domainEvents.Clear();
    }

    private void GarantirVendaAtiva()
    {
        if (Cancelada)
        {
            throw new InvalidOperationException("Não é possível alterar uma venda cancelada.");
        }
    }

    private void RegistrarEvento(IDomainEvent eventoDominio)
    {
        _domainEvents.Add(eventoDominio);
    }
}