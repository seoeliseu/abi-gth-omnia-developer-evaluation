using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence;

public static class DeveloperEvaluationDataSeeder
{
    public static async Task SeedAsync(DeveloperEvaluationDbContext context, CancellationToken cancellationToken)
    {
        var seededProducts = false;
        var seededUsers = false;
        var seededCarts = false;

        if (!await context.Products.AnyAsync(cancellationToken))
        {
            seededProducts = true;
            context.Products.AddRange(
            [
                new ProductEntity { Id = 1, Title = "Fjallraven Backpack", Price = 109.95m, Description = "Mochila para uso diário", Category = "men's clothing", Image = "https://example.com/products/1.png", RatingRate = 3.9m, RatingCount = 120, Active = true },
                new ProductEntity { Id = 2, Title = "Premium Beer Box", Price = 39.90m, Description = "Caixa de cervejas premium", Category = "beverage", Image = "https://example.com/products/2.png", RatingRate = 4.8m, RatingCount = 45, Active = true },
                new ProductEntity { Id = 3, Title = "Potato Chips", Price = 8.50m, Description = "Batata chips tamanho família", Category = "snack", Image = "https://example.com/products/3.png", RatingRate = 4.2m, RatingCount = 210, Active = true }
            ]);
        }

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            seededUsers = true;
            context.Users.AddRange(
            [
                new UserEntity { Id = 1, Email = "john@example.com", Username = "john", Password = "123456", Firstname = "John", Lastname = "Doe", City = "São Paulo", Street = "Rua A", Number = 10, Zipcode = "01000-000", GeoLat = "-23.5505", GeoLong = "-46.6333", Phone = "11999999999", Status = "Active", Role = "Customer" },
                new UserEntity { Id = 2, Email = "mary@example.com", Username = "mary", Password = "123456", Firstname = "Mary", Lastname = "Doe", City = "Campinas", Street = "Rua B", Number = 20, Zipcode = "13000-000", GeoLat = "-22.9099", GeoLong = "-47.0626", Phone = "11888888888", Status = "Active", Role = "Manager" }
            ]);
        }

        if (!await context.Carts.AnyAsync(cancellationToken))
        {
            seededCarts = true;
            context.Carts.AddRange(
            [
                new CartEntity { Id = 1, UserId = 1, Date = new DateTimeOffset(2026, 4, 24, 10, 0, 0, TimeSpan.Zero), Products = [new CartItemEntity { Id = 1, ProductId = 1, Quantity = 2 }] },
                new CartEntity { Id = 2, UserId = 2, Date = new DateTimeOffset(2026, 4, 24, 12, 0, 0, TimeSpan.Zero), Products = [new CartItemEntity { Id = 2, ProductId = 2, Quantity = 1 }, new CartItemEntity { Id = 3, ProductId = 3, Quantity = 4 }] }
            ]);
        }

        await context.SaveChangesAsync(cancellationToken);

        if (seededProducts)
        {
            await RealinharSequenciaAsync(context, "products", cancellationToken);
        }

        if (seededUsers)
        {
            await RealinharSequenciaAsync(context, "users", cancellationToken);
        }

        if (seededCarts)
        {
            await RealinharSequenciaAsync(context, "carts", cancellationToken);
            await RealinharSequenciaAsync(context, "cart_items", cancellationToken);
        }
    }

    private static Task RealinharSequenciaAsync(DeveloperEvaluationDbContext context, string tableName, CancellationToken cancellationToken)
    {
        var sql = tableName switch
        {
            "products" =>
                """
                SELECT setval(
                    pg_get_serial_sequence('products', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM products), 0), 1),
                    true);
                """,
            "users" =>
                """
                SELECT setval(
                    pg_get_serial_sequence('users', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM users), 0), 1),
                    true);
                """,
            "carts" =>
                """
                SELECT setval(
                    pg_get_serial_sequence('carts', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM carts), 0), 1),
                    true);
                """,
            "cart_items" =>
                """
                SELECT setval(
                    pg_get_serial_sequence('cart_items', 'Id'),
                    GREATEST(COALESCE((SELECT MAX("Id") FROM cart_items), 0), 1),
                    true);
                """,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, "Tabela sem sequência configurada para realinhamento.")
        };

        return context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }
}