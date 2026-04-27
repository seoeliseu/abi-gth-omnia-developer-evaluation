namespace Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

public sealed class CartItemEntity
{
    public long Id { get; set; }
    public long CartEntityId { get; set; }
    public long ProductId { get; set; }
    public int Quantity { get; set; }
}