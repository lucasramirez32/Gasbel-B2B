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

    [Fact]
    public async Task SyncProductImagesFromDiskAsync_LinksImageBySkuAndPreservesOnStartupImport()
    {
        var dbPath = $"test_img_sync_{Guid.NewGuid():N}.db";
        var tempUploadsDir = Path.Combine(Path.GetTempPath(), $"gasbel_uploads_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempUploadsDir);

        var options = new DbContextOptionsBuilder<GasbelDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var db = new GasbelDbContext(options);
        await db.Database.EnsureCreatedAsync();

        try
        {
            var csvContent = """
                Código art.,Cód.Prov.,Nombre,Stock,Precio costo,P. Venta 1,P. Venta 2,Fecha Cambio,Proveedor,Observaciones
                1415,7798356300501,AROMATIZADOR LEFRANCE 500 ML INQUIETOS,0,4000.006,6750,5200,2025-05-16,SANDRA MARZAN,
                """;

            using (var reader = new StringReader(csvContent))
            {
                await GasbelCsvImporter.ImportArticlesFromReaderAsync(db, reader, overwriteExisting: true);
            }

            // Crear imagen en disco nombrada por SKU (1415.jpg)
            var imgFilePath = Path.Combine(tempUploadsDir, "1415.jpg");
            await File.WriteAllBytesAsync(imgFilePath, [0xFF, 0xD8, 0xFF, 0xE0]);

            // Auto-vincular desde disco
            var linkedCount = await GasbelCsvImporter.SyncProductImagesFromDiskAsync(db, tempUploadsDir);
            Assert.Equal(1, linkedCount);

            var prod1415 = await db.Products.FirstOrDefaultAsync(p => p.Sku == "1415");
            Assert.NotNull(prod1415);
            Assert.NotNull(prod1415.ImageUrl);
            Assert.StartsWith("/uploads/products/1415.jpg", prod1415.ImageUrl);

            // Simular edición manual en ABM y posterior reinicio de contenedor (overwriteExisting: false)
            prod1415.Name = "AROMATIZADOR LEFRANCE 500 ML EDITADO";
            prod1415.BasePrice = 9999m;
            await db.SaveChangesAsync();

            using (var reader2 = new StringReader(csvContent))
            {
                await GasbelCsvImporter.ImportArticlesFromReaderAsync(db, reader2, overwriteExisting: false);
            }

            var prodAfterRestart = await db.Products.FirstOrDefaultAsync(p => p.Sku == "1415");
            Assert.NotNull(prodAfterRestart);
            Assert.Equal("AROMATIZADOR LEFRANCE 500 ML EDITADO", prodAfterRestart.Name);
            Assert.Equal(9999m, prodAfterRestart.BasePrice);
            Assert.StartsWith("/uploads/products/1415.jpg", prodAfterRestart.ImageUrl);
        }
        finally
        {
            await db.Database.EnsureDeletedAsync();
            if (File.Exists(dbPath)) File.Delete(dbPath);
            if (Directory.Exists(tempUploadsDir)) Directory.Delete(tempUploadsDir, recursive: true);
        }
    }
}
