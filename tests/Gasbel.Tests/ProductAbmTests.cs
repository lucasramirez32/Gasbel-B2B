using System.Text.Json;
using System.Text.Json.Serialization;
using Gasbel.Domain.Entities;
using Gasbel.Domain.Enums;
using Gasbel.Infrastructure.Persistence;
using Gasbel.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gasbel.Tests;

public class ProductAbmTests
{
    [Fact]
    public async Task UpdateProductAsync_WithLoadedNavigations_UpdatesSuccessfullyWithoutErrors()
    {
        var dbPath = $"test_abm_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<GasbelDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        try
        {
            using var db = new GasbelDbContext(options);
            await db.Database.EnsureCreatedAsync();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                Name = "Perfumería",
                Slug = "perfumeria-y-aromatizacion"
            };
            db.Categories.Add(category);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = category.Id,
                Sku = "1415",
                Name = "AROMATIZADOR LEFRANCE INQUIETOS",
                CostPrice = 4000.01m,
                BasePrice = 6750m,
                WholesalePrice = 5200m,
                IsActive = false
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();

            var repo = new CatalogRepository(db);

            // Fetch product with navigations (similar to GetProductByIdAsync)
            var trackedProduct = await repo.GetProductByIdAsync(product.Id);
            Assert.NotNull(trackedProduct);
            Assert.NotNull(trackedProduct.Category);

            // Modify fields
            trackedProduct.Name = "AROMATIZADOR LEFRANCE INQUIETOS MODIFICADO";
            trackedProduct.BasePrice = 7000m;

            // Update
            var updated = await repo.UpdateProductAsync(trackedProduct);
            Assert.Equal("AROMATIZADOR LEFRANCE INQUIETOS MODIFICADO", updated.Name);
            Assert.Equal(7000m, updated.BasePrice);

            // Verify in fresh context
            using var verifyDb = new GasbelDbContext(options);
            var saved = await verifyDb.Products.FindAsync(product.Id);
            Assert.NotNull(saved);
            Assert.Equal("AROMATIZADOR LEFRANCE INQUIETOS MODIFICADO", saved.Name);
            Assert.Equal(7000m, saved.BasePrice);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }

    [Fact]
    public void JsonSerializer_WithIgnoreCycles_DoesNotThrowOnEntityCycles()
    {
        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = "Perfumería",
            Slug = "perfumeria"
        };
        var product = new Product
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Category = category,
            Sku = "1415",
            Name = "AROMATIZADOR"
        };
        category.Products.Add(product);

        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        var json = JsonSerializer.Serialize(product, options);
        Assert.NotNull(json);
        Assert.Contains("AROMATIZADOR", json);
    }

    [Fact]
    public async Task ImportArticlesFromReaderAsync_UpdatesPricesAndPreservesPhotosAndManualStatus()
    {
        var dbPath = $"test_csv_import_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<GasbelDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        try
        {
            using (var db = new GasbelDbContext(options))
            {
                await db.Database.EnsureCreatedAsync();

                var category = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = "Perfumería y Aromatización",
                    Slug = "perfumeria-y-aromatizacion"
                };
                db.Categories.Add(category);

                // Existing product with custom photo and active status set manually by admin
                var product = new Product
                {
                    Id = Guid.NewGuid(),
                    CategoryId = category.Id,
                    Sku = "1415",
                    Name = "AROMATIZADOR LEFRANCE INQUIETOS",
                    CostPrice = 4000.01m,
                    BasePrice = 6750m,
                    WholesalePrice = 5200m,
                    IsActive = true, // Manually activated
                    ImageUrl = "/uploads/products/custom_photo.png" // Custom photo
                };
                db.Products.Add(product);
                await db.SaveChangesAsync();
            }

            // Simulate uploaded CSV with new prices
            var csvContent = @"Código art.,Cód.Prov.,Nombre,Stock,Precio costo,P. Venta 1,P. Venta 2,Fecha Cambio,Proveedor,Observaciones
1415,7798356300501,AROMATIZADOR LEFRANCE INQUIETOS 500 ML,15,4500.50,7500.00,6000.00,18/10/2026,SANDRA MARZAN,";

            using (var importDb = new GasbelDbContext(options))
            {
                using var reader = new StringReader(csvContent);
                var imported = await Gasbel.Infrastructure.Seeders.GasbelCsvImporter.ImportArticlesFromReaderAsync(importDb, reader);
                Assert.Equal(1, imported);
            }

            // Verify in fresh context
            using var verifyDb = new GasbelDbContext(options);
            var updated = await verifyDb.Products.FirstOrDefaultAsync(p => p.Sku == "1415");
            Assert.NotNull(updated);
            Assert.Equal(4500.50m, updated.CostPrice);
            Assert.Equal(7500.00m, updated.BasePrice);
            Assert.Equal(6000.00m, updated.WholesalePrice);
            Assert.Equal(15, updated.StockUnits);
            // Verify photo and active status were preserved
            Assert.Equal("/uploads/products/custom_photo.png", updated.ImageUrl);
            Assert.True(updated.IsActive);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try { File.Delete(dbPath); } catch { }
            }
        }
    }
}

