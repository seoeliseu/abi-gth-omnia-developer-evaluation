namespace Ambev.DeveloperEvaluation.ORM.Persistence.Entities;

public sealed class ProductEntity
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal RatingRate { get; set; }
    public int RatingCount { get; set; }
    public bool Active { get; set; }
}