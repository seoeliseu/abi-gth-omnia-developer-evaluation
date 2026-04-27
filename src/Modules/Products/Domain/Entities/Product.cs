using Ambev.DeveloperEvaluation.Products.Domain.ValueObjects;

namespace Ambev.DeveloperEvaluation.Products.Domain.Entities;

public sealed class Product
{
    private Product(
        long id,
        string title,
        decimal price,
        string description,
        string category,
        string image,
        ProductRating rating,
        bool active)
    {
        Id = id;
        Title = title;
        Price = price;
        Description = description;
        Category = category;
        Image = image;
        Rating = rating;
        Active = active;
    }

    public long Id { get; private set; }
    public string Title { get; private set; }
    public decimal Price { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public string Image { get; private set; }
    public ProductRating Rating { get; private set; }
    public bool Active { get; private set; }

    public static Product Criar(
        string title,
        decimal price,
        string description,
        string category,
        string image,
        ProductRating rating)
    {
        return new Product(0, title, price, description, category, image, rating, true);
    }

    public static Product Reidratar(
        long id,
        string title,
        decimal price,
        string description,
        string category,
        string image,
        ProductRating rating,
        bool active)
    {
        return new Product(id, title, price, description, category, image, rating, active);
    }

    public void AtualizarDetalhes(
        string title,
        decimal price,
        string description,
        string category,
        string image,
        ProductRating rating)
    {
        Title = title;
        Price = price;
        Description = description;
        Category = category;
        Image = image;
        Rating = rating;
    }

    public void Desativar()
    {
        Active = false;
    }
}
