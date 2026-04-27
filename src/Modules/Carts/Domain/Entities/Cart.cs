using Ambev.DeveloperEvaluation.Carts.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Carts.Domain.Entities;

public sealed class Cart
{
    private readonly List<CartItem> _produtos;

    private Cart(long id, long usuarioId, DateTimeOffset data, IEnumerable<CartItem> produtos)
    {
        Id = id;
        UsuarioId = usuarioId;
        Data = data;
        _produtos = produtos.ToList();
    }

    public long Id { get; private set; }
    public long UsuarioId { get; private set; }
    public DateTimeOffset Data { get; private set; }
    public IReadOnlyCollection<CartItem> Produtos => _produtos;

    public static Cart Criar(long usuarioId, DateTimeOffset data, IEnumerable<CartItem> produtos)
        => new(0, usuarioId, data, produtos);

    public static Cart Reidratar(long id, long usuarioId, DateTimeOffset data, IEnumerable<CartItem> produtos)
        => new(id, usuarioId, data, produtos);

    public void Atualizar(long usuarioId, DateTimeOffset data, IEnumerable<CartItem> produtos)
    {
        UsuarioId = usuarioId;
        Data = data;
        _produtos.Clear();
        _produtos.AddRange(produtos);
    }
}
