using Gasbel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gasbel.Infrastructure.Persistence;

public class GasbelDbContext : DbContext
{
    public GasbelDbContext(DbContextOptions<GasbelDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CustomerGroup> CustomerGroups => Set<CustomerGroup>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TierPrice> TierPrices => Set<TierPrice>();
    public DbSet<GroupPrice> GroupPrices => Set<GroupPrice>();
    public DbSet<CustomerPrice> CustomerPrices => Set<CustomerPrice>();
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Category
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.Name).IsRequired().HasMaxLength(150);
            entity.Property(c => c.Slug).IsRequired().HasMaxLength(150);
            entity.HasIndex(c => c.Slug).IsUnique();
        });

        // CustomerGroup
        modelBuilder.Entity<CustomerGroup>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Code).IsRequired();
            entity.Property(g => g.Name).IsRequired().HasMaxLength(100);
            entity.Property(g => g.DefaultDiscountPercentage).HasPrecision(5, 2);
            entity.HasIndex(g => g.Code).IsUnique();
        });

        // Customer
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.BusinessName).IsRequired().HasMaxLength(200);
            entity.Property(c => c.TaxId).IsRequired().HasMaxLength(50);
            entity.Property(c => c.Email).HasMaxLength(150);
            entity.Property(c => c.Phone).HasMaxLength(50);

            entity.HasOne(c => c.CustomerGroup)
                  .WithMany(g => g.Customers)
                  .HasForeignKey(c => c.CustomerGroupId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Sku).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(250);
            entity.Property(p => p.BasePrice).HasPrecision(18, 2);
            entity.HasIndex(p => p.Sku).IsUnique();
            entity.HasIndex(p => p.CategoryId);

            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // TierPrice
        modelBuilder.Entity<TierPrice>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(t => t.Product)
                  .WithMany(p => p.TierPrices)
                  .HasForeignKey(t => t.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(t => t.CustomerGroup)
                  .WithMany(g => g.TierPrices)
                  .HasForeignKey(t => t.CustomerGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(t => new { t.ProductId, t.CustomerGroupId, t.MinQuantity });
        });

        // GroupPrice
        modelBuilder.Entity<GroupPrice>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(g => g.Product)
                  .WithMany(p => p.GroupPrices)
                  .HasForeignKey(g => g.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(g => g.CustomerGroup)
                  .WithMany(cg => cg.GroupPrices)
                  .HasForeignKey(g => g.CustomerGroupId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(g => new { g.ProductId, g.CustomerGroupId }).IsUnique();
        });

        // CustomerPrice
        modelBuilder.Entity<CustomerPrice>(entity =>
        {
            entity.HasKey(cp => cp.Id);
            entity.Property(cp => cp.UnitPrice).HasPrecision(18, 2);

            entity.HasOne(cp => cp.Product)
                  .WithMany(p => p.CustomerPrices)
                  .HasForeignKey(cp => cp.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cp => cp.Customer)
                  .WithMany(c => c.CustomerPrices)
                  .HasForeignKey(cp => cp.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cp => new { cp.ProductId, cp.CustomerId }).IsUnique();
        });

        // Quote
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(q => q.Id);
            entity.Property(q => q.Subtotal).HasPrecision(18, 2);
            entity.Property(q => q.TaxAmount).HasPrecision(18, 2);
            entity.Property(q => q.Total).HasPrecision(18, 2);
            entity.Property(q => q.CustomerName).HasMaxLength(200);

            entity.HasOne(q => q.Customer)
                  .WithMany(c => c.Quotes)
                  .HasForeignKey(q => q.CustomerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // QuoteItem
        modelBuilder.Entity<QuoteItem>(entity =>
        {
            entity.HasKey(qi => qi.Id);
            entity.Property(qi => qi.AppliedUnitPrice).HasPrecision(18, 2);
            entity.Property(qi => qi.LineSubtotal).HasPrecision(18, 2);

            entity.HasOne(qi => qi.Quote)
                  .WithMany(q => q.Items)
                  .HasForeignKey(qi => qi.QuoteId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(qi => qi.Product)
                  .WithMany()
                  .HasForeignKey(qi => qi.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
