using System.Globalization;
using System.Text.RegularExpressions;
using Gasbel.Application.Services;
using Gasbel.Domain.Entities;
using Gasbel.Domain.Enums;
using Gasbel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasbel.Infrastructure.Seeders;

public static class GasbelCsvImporter
{
    private static readonly string[] AllowedImageExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public static string SanitizeSkuForFileName(string? sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return string.Empty;
        }

        var trimmed = sku.Trim();
        var invalidChars = Path.GetInvalidFileNameChars();
        var sb = new System.Text.StringBuilder(trimmed.Length);
        foreach (var ch in trimmed)
        {
            if (Array.IndexOf(invalidChars, ch) >= 0 || char.IsWhiteSpace(ch))
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    public static async Task<int> ImportArticlesFromCsvAsync(
        GasbelDbContext context,
        string csvFilePath,
        bool overwriteExisting = true)
    {
        if (!File.Exists(csvFilePath))
        {
            throw new FileNotFoundException($"El archivo CSV no existe en la ruta: {csvFilePath}");
        }

        using var reader = new StreamReader(csvFilePath);
        return await ImportArticlesFromReaderAsync(context, reader, overwriteExisting);
    }

    public static async Task<int> ImportArticlesFromReaderAsync(
        GasbelDbContext context,
        TextReader reader,
        bool overwriteExisting = true)
    {
        // 1. Asegurar Grupos de clientes
        var groupRetail = await context.CustomerGroups.FirstOrDefaultAsync(g => g.Code == CustomerGroupCode.Retail);
        if (groupRetail == null)
        {
            groupRetail = new CustomerGroup
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Code = CustomerGroupCode.Retail,
                Name = "Minorista (Público General)",
                Description = "Clientes particulares y compras al por menor a precio de lista.",
                DefaultDiscountPercentage = 0m
            };
            context.CustomerGroups.Add(groupRetail);
        }

        var groupGastronomy = await context.CustomerGroups.FirstOrDefaultAsync(g => g.Code == CustomerGroupCode.Gastronomy);
        if (groupGastronomy == null)
        {
            groupGastronomy = new CustomerGroup
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Code = CustomerGroupCode.Gastronomy,
                Name = "Comercio Gastronómico",
                Description = "Rotiserías, pizzerías, restaurantes y casas de comidas con precios preferenciales.",
                DefaultDiscountPercentage = 12m
            };
            context.CustomerGroups.Add(groupGastronomy);
        }

        var groupWholesale = await context.CustomerGroups.FirstOrDefaultAsync(g => g.Code == CustomerGroupCode.WholesaleDistributor);
        if (groupWholesale == null)
        {
            groupWholesale = new CustomerGroup
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Code = CustomerGroupCode.WholesaleDistributor,
                Name = "Distribuidor / Reventa",
                Description = "Papeleras locales y distribuidores con precio mayorista por bulto cerrado.",
                DefaultDiscountPercentage = 22m
            };
            context.CustomerGroups.Add(groupWholesale);
        }

        var groupKeyAccount = await context.CustomerGroups.FirstOrDefaultAsync(g => g.Code == CustomerGroupCode.KeyAccount);
        if (groupKeyAccount == null)
        {
            groupKeyAccount = new CustomerGroup
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                Code = CustomerGroupCode.KeyAccount,
                Name = "Gran Cuenta / Corporativo",
                Description = "Cadenas, franquicias y clientes con acuerdos contractuales especiales.",
                DefaultDiscountPercentage = 28m
            };
            context.CustomerGroups.Add(groupKeyAccount);
        }

        await context.SaveChangesAsync();

        // 2. Asegurar Categorías
        var categoriesDict = await context.Categories.ToDictionaryAsync(c => c.Name, StringComparer.OrdinalIgnoreCase);

        var requiredCategories = new (string Name, string Slug, string Desc, int Order)[]
        {
            ("Bolsas de Polietileno, Papel y Residuos", "bolsas-polietileno-papel-residuos", "Bolsas camiseta alta/baja densidad, arranque en rollo, consorcio pesadas y papel Kraft.", 1),
            ("Envases, Bandejas Térmicas y Packaging Gastronómico", "envases-bandejas-packaging-gastronomico", "Bandejas EPS, aluminio con tapa, cajas microcorrugadas, moldes y potes térmicos.", 2),
            ("Vasos, Copas, Cubiertos y Artículos de Mesa", "vasos-copas-cubiertos-mesa", "Vasos plásticos, polipapel, platos descartables, copas y cubiertos reforzados.", 3),
            ("Papelera Comercial, Films de Embalaje e Higiene", "papelera-films-embalaje-higiene", "Film stretch virgen, film PVC gastronómico, servilletas y toallas higiénicas.", 4),
            ("Perfumería y Aromatización", "perfumeria-y-aromatizacion", "Difusores ambientales, aromatizadores, jabones y perfumería para el hogar y vehículos.", 5),
            ("Artículos Varios y Bazar", "articulos-varios-y-bazar", "Artículos complementarios, precintos, cintas, guantes, velas y bazar comercial.", 6)
        };

        foreach (var reqCat in requiredCategories)
        {
            if (!categoriesDict.TryGetValue(reqCat.Name, out var cat))
            {
                cat = new Category
                {
                    Id = Guid.NewGuid(),
                    Name = reqCat.Name,
                    Slug = reqCat.Slug,
                    Description = reqCat.Desc,
                    DisplayOrder = reqCat.Order
                };
                context.Categories.Add(cat);
                categoriesDict[reqCat.Name] = cat;
            }
        }

        await context.SaveChangesAsync();

        // 3. Leer y parsear líneas del CSV
        var lines = new List<string>();
        string? currentLine;
        while ((currentLine = await reader.ReadLineAsync()) != null)
        {
            lines.Add(currentLine);
        }

        if (lines.Count <= 1)
        {
            return 0;
        }

        int importedCount = 0;
        var existingProducts = await context.Products
            .Include(p => p.GroupPrices)
            .Include(p => p.TierPrices)
            .ToDictionaryAsync(p => p.Sku);

        // Header: Código art.,Cód.Prov.,Nombre,Stock,Precio costo,P. Venta 1,P. Venta 2,Fecha Cambio,Proveedor,Observaciones
        for (int i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Manejo de comas dentro de comillas
            var tokens = ParseCsvLine(line);
            if (tokens.Count < 3) continue;

            string sku = tokens[0].Trim();
            if (string.IsNullOrWhiteSpace(sku)) continue;

            string supplierCode = tokens.Count > 1 ? tokens[1].Trim() : string.Empty;
            string rawName = tokens.Count > 2 ? tokens[2].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(rawName)) continue;

            int stock = 0;
            if (tokens.Count > 3 && int.TryParse(tokens[3].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedStock))
            {
                stock = parsedStock;
            }

            decimal costPrice = 0m;
            if (tokens.Count > 4 && decimal.TryParse(tokens[4].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedCost))
            {
                costPrice = Math.Round(parsedCost, 2);
            }

            decimal pVenta1 = 0m;
            if (tokens.Count > 5 && decimal.TryParse(tokens[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVenta1))
            {
                pVenta1 = Math.Round(parsedVenta1, 2);
            }

            decimal pVenta2 = 0m;
            if (tokens.Count > 6 && decimal.TryParse(tokens[6].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedVenta2))
            {
                pVenta2 = Math.Round(parsedVenta2, 2);
            }

            string supplier = tokens.Count > 8 ? tokens[8].Trim() : string.Empty;
            string notes = tokens.Count > 9 ? tokens[9].Trim() : string.Empty;

            // Parser de atributos y dimensiones
            var parsedAttrs = ProductSpecificationParser.Parse(rawName, supplier);
            bool isPerfume = ProductSpecificationParser.IsPerfumery(rawName, supplier);

            // Categoría asignada
            string catName = parsedAttrs.SuggestedCategory ?? "Artículos Varios y Bazar";
            if (!categoriesDict.TryGetValue(catName, out var targetCategory))
            {
                targetCategory = categoriesDict["Artículos Varios y Bazar"];
            }

            // Unidades por caja / pack inferidas
            int unitsPerBox = 1;
            if (!string.IsNullOrWhiteSpace(parsedAttrs.PackageQuantity))
            {
                var numMatch = Regex.Match(parsedAttrs.PackageQuantity, @"\d+");
                if (numMatch.Success && int.TryParse(numMatch.Value, out var num) && num > 1)
                {
                    unitsPerBox = num;
                }
            }

            // Estado: Si es de perfumería se migra desactivado por solicitud del cliente
            bool isActive = !isPerfume;

            if (existingProducts.TryGetValue(sku, out var product))
            {
                if (!overwriteExisting)
                {
                    // En arranque normal (startup) no pisamos ningún atributo de productos ya existentes en la base
                    continue;
                }

                // Actualizar producto existente (precios, stock y especificaciones) preservando personalizaciones
                product.SupplierCode = !string.IsNullOrWhiteSpace(supplierCode) ? supplierCode : product.SupplierCode;
                product.Name = parsedAttrs.CleanName;
                product.Description = rawName;
                product.Dimensions = !string.IsNullOrWhiteSpace(parsedAttrs.Dimensions) ? parsedAttrs.Dimensions : product.Dimensions;
                product.Capacity = !string.IsNullOrWhiteSpace(parsedAttrs.Capacity) ? parsedAttrs.Capacity : product.Capacity;
                product.PackageQuantity = !string.IsNullOrWhiteSpace(parsedAttrs.PackageQuantity) ? parsedAttrs.PackageQuantity : product.PackageQuantity;
                product.Supplier = supplier;
                product.Notes = notes;
                product.CostPrice = costPrice;
                product.BasePrice = pVenta1 > 0 ? pVenta1 : product.BasePrice;
                product.WholesalePrice = pVenta2 > 0 ? pVenta2 : product.BasePrice;
                product.StockUnits = stock;
                product.UnitsPerBox = unitsPerBox > 1 ? unitsPerBox : product.UnitsPerBox;
                product.CategoryId = targetCategory.Id;
                // NOTA: No sobreescribimos product.IsActive ni product.ImageUrl para preservar personalizaciones manuales
            }
            else
            {
                // Crear producto nuevo
                product = new Product
                {
                    Id = Guid.NewGuid(),
                    CategoryId = targetCategory.Id,
                    Sku = sku,
                    SupplierCode = supplierCode,
                    Name = parsedAttrs.CleanName,
                    Description = rawName,
                    Dimensions = parsedAttrs.Dimensions,
                    Capacity = parsedAttrs.Capacity,
                    PackageQuantity = parsedAttrs.PackageQuantity,
                    Supplier = supplier,
                    Notes = notes,
                    UnitOfMeasure = UnitOfMeasure.Package,
                    PackagingType = PackagingType.Box,
                    UnitsPerBox = unitsPerBox,
                    CostPrice = costPrice,
                    BasePrice = pVenta1 > 0 ? pVenta1 : (pVenta2 > 0 ? pVenta2 : 1m),
                    WholesalePrice = pVenta2 > 0 ? pVenta2 : (pVenta1 > 0 ? pVenta1 : 1m),
                    StockUnits = stock,
                    IsActive = isActive,
                    ImageUrl = null
                };

                context.Products.Add(product);
                existingProducts[sku] = product;
            }

            // Precios de grupo: Asignar precio de venta 2 a Gastronomía y Distribuidor
            if (pVenta2 > 0 && pVenta2 < product.BasePrice)
            {
                var gpGastro = product.GroupPrices.FirstOrDefault(g => g.CustomerGroupId == groupGastronomy.Id);
                if (gpGastro == null)
                {
                    var gp = new GroupPrice
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        CustomerGroupId = groupGastronomy.Id,
                        UnitPrice = pVenta2
                    };
                    context.GroupPrices.Add(gp);
                    product.GroupPrices.Add(gp);
                }
                else
                {
                    gpGastro.UnitPrice = pVenta2;
                }

                var gpWholesale = product.GroupPrices.FirstOrDefault(g => g.CustomerGroupId == groupWholesale.Id);
                if (gpWholesale == null)
                {
                    var gp = new GroupPrice
                    {
                        Id = Guid.NewGuid(),
                        ProductId = product.Id,
                        CustomerGroupId = groupWholesale.Id,
                        UnitPrice = pVenta2
                    };
                    context.GroupPrices.Add(gp);
                    product.GroupPrices.Add(gp);
                }
                else
                {
                    gpWholesale.UnitPrice = pVenta2;
                }
            }

            // Descuento por bulto cerrado si unitsPerBox > 1
            if (unitsPerBox > 1 && !product.TierPrices.Any(t => t.MinQuantity >= unitsPerBox))
            {
                decimal tierUnitPrice = pVenta2 > 0 ? pVenta2 : Math.Round(product.BasePrice * 0.90m, 2);
                var tp = new TierPrice
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    CustomerGroupId = null, // Aplica a todos por bulto cerrado
                    MinQuantity = unitsPerBox,
                    UnitPrice = tierUnitPrice
                };
                context.TierPrices.Add(tp);
                product.TierPrices.Add(tp);
            }

            importedCount++;
        }

        await context.SaveChangesAsync();
        return importedCount;
    }

    /// <summary>
    /// Escanea la carpeta física de imágenes (wwwroot/uploads/products) y revincula automáticamente
    /// los archivos existentes por SKU (ej: 1415.jpg) o por Id de producto si el campo ImageUrl está vacío o desincronizado.
    /// </summary>
    public static async Task<int> SyncProductImagesFromDiskAsync(GasbelDbContext context, string uploadsProductsDir)
    {
        if (!Directory.Exists(uploadsProductsDir))
        {
            return 0;
        }

        var files = Directory.GetFiles(uploadsProductsDir)
            .Where(f => AllowedImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();

        if (files.Count == 0)
        {
            return 0;
        }

        var fileMapByBaseName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var baseName = Path.GetFileNameWithoutExtension(file);
            if (!string.IsNullOrWhiteSpace(baseName))
            {
                fileMapByBaseName[baseName] = file;
            }
        }

        var products = await context.Products.ToListAsync();
        int updatedCount = 0;

        foreach (var product in products)
        {
            // Verificar si ya tiene un ImageUrl local que apunta a un archivo inexistente
            if (!string.IsNullOrWhiteSpace(product.ImageUrl) &&
                product.ImageUrl.StartsWith("/uploads/products/", StringComparison.OrdinalIgnoreCase))
            {
                var cleanPath = product.ImageUrl.Split('?')[0];
                var existingFileName = Path.GetFileName(cleanPath);
                var expectedFullPath = Path.Combine(uploadsProductsDir, existingFileName);
                if (File.Exists(expectedFullPath))
                {
                    continue; // La imagen local existe y es válida
                }

                // El archivo referenciado ya no existe con ese nombre; intentaremos revincular por SKU o ID
                product.ImageUrl = null;
            }

            if (string.IsNullOrWhiteSpace(product.ImageUrl))
            {
                var safeSku = SanitizeSkuForFileName(product.Sku);
                string? matchedFile = null;

                if (!string.IsNullOrWhiteSpace(safeSku) && fileMapByBaseName.TryGetValue(safeSku, out var bySku))
                {
                    matchedFile = bySku;
                }
                else if (fileMapByBaseName.TryGetValue(product.Id.ToString(), out var byId))
                {
                    matchedFile = byId;
                }

                if (matchedFile != null)
                {
                    var fileName = Path.GetFileName(matchedFile);
                    var ticks = File.GetLastWriteTimeUtc(matchedFile).Ticks;
                    product.ImageUrl = $"/uploads/products/{fileName}?v={ticks}";
                    updatedCount++;
                }
            }
        }

        if (updatedCount > 0)
        {
            await context.SaveChangesAsync();
        }

        return updatedCount;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var currentToken = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentToken.ToString());
                currentToken.Clear();
            }
            else
            {
                currentToken.Append(c);
            }
        }

        result.Add(currentToken.ToString());
        return result;
    }
}
