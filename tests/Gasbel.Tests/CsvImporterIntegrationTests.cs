using Gasbel.Domain.Enums;
using Gasbel.Infrastructure.Persistence;
using Gasbel.Infrastructure.Seeders;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gasbel.Tests;

public class CsvImporterIntegrationTests
{
    [Fact]
    public async Task ImportArticlesFromCsvAsync_ImportsAllArticlesAndSetsPerfumeryInactive()
    {
        var dbPath = $"test_gasbel_{Guid.NewGuid():N}.db";
        var options = new DbContextOptionsBuilder<GasbelDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var db = new GasbelDbContext(options);
        await db.Database.EnsureCreatedAsync();

        string? csvPath = null;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "data", "articulos_gasbel.csv");
            if (File.Exists(candidate))
            {
                csvPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        Assert.True(csvPath != null && File.Exists(csvPath), "El archivo data/articulos_gasbel.csv debe existir en la solución");

        // Ejecutar importación
        var importedCount = await GasbelCsvImporter.ImportArticlesFromCsvAsync(db, csvPath);

        try
        {
            // Validaciones
            Assert.True(importedCount >= 280, $"Se debieron importar al menos 280 artículos. Total importados: {importedCount}");

            var total = await db.Products.CountAsync();
            Assert.Equal(importedCount, total);

            // Perfumería: Sandra Marzan debe estar desactivado (IsActive == false)
            var perfumeryCount = await db.Products
                .Where(p => p.Category.Slug == "perfumeria-y-aromatizacion" || (p.Supplier != null && p.Supplier.Contains("SANDRA MARZAN")))
                .CountAsync();

            Assert.True(perfumeryCount > 50, $"Se esperaban más de 50 artículos de perfumería. Encontrados: {perfumeryCount}");

            var activePerfumeryCount = await db.Products
                .Where(p => (p.Category.Slug == "perfumeria-y-aromatizacion" || (p.Supplier != null && p.Supplier.Contains("SANDRA MARZAN"))) && p.IsActive)
                .CountAsync();

            Assert.Equal(0, activePerfumeryCount); // Ningún artículo de perfumería debe estar activo

            // Bolsas y descartables deben tener medidas extraídas
            var sampleBolsa = await db.Products.FirstOrDefaultAsync(p => p.Sku == "331"); // ARRANQUE COMUN 20X30
            Assert.NotNull(sampleBolsa);
            Assert.Contains("20X30", sampleBolsa.Dimensions);
            Assert.True(sampleBolsa.IsActive);
            Assert.True(sampleBolsa.BasePrice > 0);
            Assert.True(sampleBolsa.WholesalePrice > 0);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    [Fact]
    public async Task SeedMainDatabaseFromCsvAsync()
    {
        string? root = null;
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "data", "articulos_gasbel.csv")))
            {
                root = dir.FullName;
                break;
            }
            dir = dir.Parent;
        }

        Assert.NotNull(root);
        var csvPath = Path.Combine(root, "data", "articulos_gasbel.csv");
        var mainDbPath = Path.Combine(root, "src", "Gasbel.Web", "gasbel_b2b.db");

        var options = new DbContextOptionsBuilder<GasbelDbContext>()
            .UseSqlite($"Data Source={mainDbPath}")
            .Options;

        using var db = new GasbelDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var count = await GasbelCsvImporter.ImportArticlesFromCsvAsync(db, csvPath);
        Assert.True(count >= 280, $"Se deben haber importado al menos 280 artículos en gasbel_b2b.db. Total: {count}");

        var total = await db.Products.CountAsync();
        Assert.True(total >= 280);
    }
}
