namespace Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

public sealed class CartEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public DateTimeOffset Date { get; set; }
    public List<CartItemEntity> Products { get; set; } = [];
}