using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Sales.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence;

public sealed class DeveloperEvaluationDbContext : DbContext
{
    public DeveloperEvaluationDbContext(DbContextOptions<DeveloperEvaluationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<CartEntity> Carts => Set<CartEntity>();
    public DbSet<CartItemEntity> CartItems => Set<CartItemEntity>();
    public DbSet<IdempotencyEntryEntity> IdempotencyEntries => Set<IdempotencyEntryEntity>();
    public DbSet<OutboxMessageEntity> OutboxMessages => Set<OutboxMessageEntity>();
    public DbSet<ProcessedMessageEntity> ProcessedMessages => Set<ProcessedMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sale>(builder =>
        {
            builder.ToTable("sales");
            builder.HasKey(sale => sale.Id);
            builder.Property(sale => sale.Numero).HasMaxLength(64).IsRequired();
            builder.Property(sale => sale.ClienteNome).HasMaxLength(200).IsRequired();
            builder.Property(sale => sale.FilialNome).HasMaxLength(200).IsRequired();
            builder.Ignore(sale => sale.DomainEvents);
            builder.Ignore(sale => sale.ValorTotal);

            builder.OwnsMany(sale => sale.Items, itemBuilder =>
            {
                itemBuilder.ToTable("sale_items");
                itemBuilder.WithOwner().HasForeignKey("SaleId");
                itemBuilder.HasKey(item => item.Id);
                itemBuilder.Property(item => item.ProductTitle).HasMaxLength(300).IsRequired();
                itemBuilder.Ignore(item => item.ValorBruto);
                itemBuilder.Ignore(item => item.ValorDesconto);
                itemBuilder.Ignore(item => item.ValorTotal);
            });

            builder.Navigation(sale => sale.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ProductEntity>(builder =>
        {
            builder.ToTable("products");
            builder.HasKey(product => product.Id);
            builder.Property(product => product.Title).HasMaxLength(250).IsRequired();
            builder.Property(product => product.Category).HasMaxLength(120).IsRequired();
            builder.Property(product => product.Image).HasMaxLength(500).IsRequired();
            builder.Property(product => product.Description).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<UserEntity>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Email).HasMaxLength(200).IsRequired();
            builder.Property(user => user.Username).HasMaxLength(120).IsRequired();
            builder.Property(user => user.Password).HasMaxLength(200).IsRequired();
            builder.Property(user => user.Status).HasMaxLength(40).IsRequired();
            builder.Property(user => user.Role).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<CartEntity>(builder =>
        {
            builder.ToTable("carts");
            builder.HasKey(cart => cart.Id);
            builder.HasMany(cart => cart.Products)
                .WithOne()
                .HasForeignKey(item => item.CartEntityId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartItemEntity>(builder =>
        {
            builder.ToTable("cart_items");
            builder.HasKey(item => item.Id);
        });

        modelBuilder.Entity<IdempotencyEntryEntity>(builder =>
        {
            builder.ToTable("idempotency_entries");
            builder.HasKey(entry => entry.Id);
            builder.HasIndex(entry => new { entry.Scope, entry.Key }).IsUnique();
            builder.Property(entry => entry.Scope).HasMaxLength(100).IsRequired();
            builder.Property(entry => entry.Key).HasMaxLength(200).IsRequired();
            builder.Property(entry => entry.Fingerprint).HasMaxLength(500).IsRequired();
            builder.Property(entry => entry.ResultType).HasMaxLength(500).IsRequired();
            builder.Property(entry => entry.ResultPayload).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<OutboxMessageEntity>(builder =>
        {
            builder.ToTable("outbox_messages");
            builder.HasKey(message => message.Id);
            builder.Property(message => message.AggregateType).HasMaxLength(200).IsRequired();
            builder.Property(message => message.AggregateId).HasMaxLength(200).IsRequired();
            builder.Property(message => message.EventType).HasMaxLength(200).IsRequired();
            builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        });

        modelBuilder.Entity<ProcessedMessageEntity>(builder =>
        {
            builder.ToTable("processed_messages");
            builder.HasKey(message => message.Id);
            builder.HasIndex(message => new { message.Consumer, message.MessageId }).IsUnique();
            builder.Property(message => message.Consumer).HasMaxLength(200).IsRequired();
            builder.Property(message => message.MessageId).HasMaxLength(200).IsRequired();
        });
    }
}