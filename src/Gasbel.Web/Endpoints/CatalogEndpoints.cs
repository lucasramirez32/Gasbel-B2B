using Gasbel.Application.DTOs;
using Gasbel.Application.Interfaces;
using Gasbel.Application.Pricing;
using Gasbel.Application.Quotes;
using Gasbel.Application.Services;
using Gasbel.Domain.Entities;
using Gasbel.Domain.Enums;
using Gasbel.Infrastructure.Persistence;
using Gasbel.Infrastructure.Seeders;
using Microsoft.AspNetCore.Mvc;

namespace Gasbel.Web.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Gasbel B2B Catalog & Quotes");

        // Categories
        group.MapGet("/categories", async (ICatalogRepository catalogRepo, CancellationToken ct) =>
        {
            var categories = await catalogRepo.GetCategoriesAsync(ct);
            var dtos = categories.Select(c => new CategoryDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.DisplayOrder,
                c.Products.Count(p => p.IsActive)
            )).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetCategories")
        .WithSummary("Obtiene las familias de productos de descartables y perfumería");

        // Customer Groups (para el simulador B2B)
        group.MapGet("/customer-groups", async (ICatalogRepository catalogRepo, CancellationToken ct) =>
        {
            var groups = await catalogRepo.GetCustomerGroupsAsync(ct);
            var dtos = groups.Select(g => new CustomerGroupDto(
                g.Id,
                g.Code.ToString(),
                g.Name,
                g.Description,
                g.DefaultDiscountPercentage
            )).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetCustomerGroups")
        .WithSummary("Obtiene los grupos comerciales de clientes");

        // Customers (para el simulador B2B)
        group.MapGet("/customers", async (ICatalogRepository catalogRepo, CancellationToken ct) =>
        {
            var customers = await catalogRepo.GetCustomersAsync(ct);
            var dtos = customers.Select(c => new CustomerDto(
                c.Id,
                c.CustomerGroupId,
                c.CustomerGroup?.Name ?? "General",
                c.BusinessName,
                c.TaxId,
                c.Email,
                c.Phone
            )).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetCustomers")
        .WithSummary("Obtiene clientes de ejemplo registrados para simulación");

        // Products (Catálogo Público / Clientes)
        group.MapGet("/products", async (
            [FromQuery] Guid? categoryId,
            [FromQuery] string? search,
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? customerGroupId,
            ICatalogRepository catalogRepo,
            IPricingService pricingService,
            CancellationToken ct) =>
        {
            var products = await catalogRepo.GetProductsAsync(categoryId, search, ct);

            var dtos = products.Select(p =>
            {
                var priceRes = pricingService.CalculatePrice(p, quantity: 1, customerId, customerGroupId);
                return MapToProductDto(p, priceRes);
            }).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetProducts")
        .WithSummary("Lista productos activos con resolución de precios según contexto de cliente");

        // Product Details
        group.MapGet("/products/{id:guid}", async (
            Guid id,
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? customerGroupId,
            ICatalogRepository catalogRepo,
            IPricingService pricingService,
            CancellationToken ct) =>
        {
            var product = await catalogRepo.GetProductByIdAsync(id, ct);
            if (product == null)
            {
                return Results.NotFound();
            }

            var priceRes = pricingService.CalculatePrice(product, quantity: 1, customerId, customerGroupId);
            return Results.Ok(MapToProductDto(product, priceRes));
        })
        .WithName("GetProductById")
        .WithSummary("Detalle individual de producto y sus escalas de precio");

        // Dynamic Pricing Calculation
        group.MapPost("/pricing/calculate", async (
            [FromBody] CalculatePriceRequest req,
            ICatalogRepository catalogRepo,
            IPricingService pricingService,
            CancellationToken ct) =>
        {
            var product = await catalogRepo.GetProductByIdAsync(req.ProductId, ct);
            if (product == null)
            {
                return Results.NotFound(new { Message = "Producto no encontrado" });
            }

            var result = pricingService.CalculatePrice(product, req.Quantity, req.CustomerId, req.CustomerGroupId);
            return Results.Ok(result);
        })
        .WithName("CalculatePrice")
        .WithSummary("Calcula el precio unitario, subtotal y oportunidades de ahorro para una cantidad");

        // Quotes
        group.MapPost("/quotes", async (
            [FromBody] CreateQuoteRequest req,
            IQuoteService quoteService,
            CancellationToken ct) =>
        {
            if (req.Items == null || req.Items.Count == 0)
            {
                return Results.BadRequest(new { Message = "Debe incluir al menos un ítem en la cotización" });
            }

            var quote = await quoteService.CreateQuoteAsync(req, ct);
            return Results.Created($"/api/quotes/{quote.Id}", quote);
        })
        .WithName("CreateQuote")
        .WithSummary("Crea un presupuesto formal o cotización de pedido");

        group.MapGet("/quotes/{id:guid}", async (
            Guid id,
            IQuoteService quoteService,
            CancellationToken ct) =>
        {
            var quote = await quoteService.GetQuoteByIdAsync(id, ct);
            return quote != null ? Results.Ok(quote) : Results.NotFound();
        })
        .WithName("GetQuoteById")
        .WithSummary("Consulta una cotización por su identificador");

        // ==========================================
        // ENDPOINTS ABM Y ADMINISTRACIÓN DE PRODUCTOS
        // ==========================================

        // Listado para Admin / ABM
        group.MapGet("/admin/products", async (
            [FromQuery] string? search,
            [FromQuery] Guid? categoryId,
            [FromQuery] string? filterType,
            ICatalogRepository catalogRepo,
            IPricingService pricingService,
            CancellationToken ct) =>
        {
            var products = await catalogRepo.GetAdminProductsAsync(search, categoryId, filterType, ct);
            var dtos = products.Select(p =>
            {
                var priceRes = pricingService.CalculatePrice(p, quantity: 1, null, null);
                return MapToProductDto(p, priceRes);
            }).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetAdminProducts")
        .WithSummary("Lista completa de productos para el ABM con filtros avanzados (activos, inactivos, sin foto, etc.)");

        // Estadísticas de Catálogo
        group.MapGet("/admin/stats", async (ICatalogRepository catalogRepo, CancellationToken ct) =>
        {
            var stats = await catalogRepo.GetStatsAsync(ct);
            return Results.Ok(stats);
        })
        .WithName("GetAdminStats")
        .WithSummary("Estadísticas cuantitativas del inventario y catálogo");

        // Asistente Inteligente de Detección de Especificaciones
        group.MapPost("/admin/products/parse-attributes", ([FromBody] ParseAttributesRequest req) =>
        {
            var parsed = ProductSpecificationParser.Parse(req.Title, req.Supplier);
            return Results.Ok(parsed);
        })
        .WithName("ParseProductAttributes")
        .WithSummary("Extrae automáticamente dimensiones, capacidad, pack y categoría sugerida de un título");

        // Crear Producto en ABM
        group.MapPost("/admin/products", async (
            [FromBody] CreateProductRequest req,
            ICatalogRepository catalogRepo,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Sku) || string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { Message = "El SKU y el Nombre son obligatorios." });
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                CategoryId = req.CategoryId,
                Sku = req.Sku.Trim(),
                SupplierCode = req.SupplierCode?.Trim(),
                Name = req.Name.Trim(),
                Description = req.Description ?? string.Empty,
                Dimensions = req.Dimensions?.Trim(),
                Capacity = req.Capacity?.Trim(),
                PackageQuantity = req.PackageQuantity?.Trim(),
                Supplier = req.Supplier?.Trim(),
                Notes = req.Notes?.Trim(),
                UnitsPerBox = req.UnitsPerBox > 0 ? req.UnitsPerBox : 1,
                CostPrice = req.CostPrice,
                BasePrice = req.BasePrice,
                WholesalePrice = req.WholesalePrice > 0 ? req.WholesalePrice : req.BasePrice,
                StockUnits = req.StockUnits,
                IsActive = req.IsActive,
                ImageUrl = req.ImageUrl
            };

            await catalogRepo.AddProductAsync(product, ct);
            return Results.Created($"/api/products/{product.Id}", product);
        })
        .WithName("CreateProduct")
        .WithSummary("Crea un nuevo producto en el catálogo");

        // Modificar Producto en ABM
        group.MapPut("/admin/products/{id:guid}", async (
            Guid id,
            [FromBody] UpdateProductRequest req,
            ICatalogRepository catalogRepo,
            CancellationToken ct) =>
        {
            var product = await catalogRepo.GetProductByIdAsync(id, ct);
            if (product == null)
            {
                return Results.NotFound(new { Message = "Producto no encontrado." });
            }

            product.CategoryId = req.CategoryId;
            product.Sku = req.Sku.Trim();
            product.SupplierCode = req.SupplierCode?.Trim();
            product.Name = req.Name.Trim();
            product.Description = req.Description ?? string.Empty;
            product.Dimensions = req.Dimensions?.Trim();
            product.Capacity = req.Capacity?.Trim();
            product.PackageQuantity = req.PackageQuantity?.Trim();
            product.Supplier = req.Supplier?.Trim();
            product.Notes = req.Notes?.Trim();
            product.UnitsPerBox = req.UnitsPerBox > 0 ? req.UnitsPerBox : 1;
            product.CostPrice = req.CostPrice;
            product.BasePrice = req.BasePrice;
            product.WholesalePrice = req.WholesalePrice > 0 ? req.WholesalePrice : req.BasePrice;
            product.StockUnits = req.StockUnits;
            product.IsActive = req.IsActive;
            if (!string.IsNullOrWhiteSpace(req.ImageUrl))
            {
                product.ImageUrl = req.ImageUrl;
            }

            await catalogRepo.UpdateProductAsync(product, ct);
            return Results.Ok(product);
        })
        .WithName("UpdateProduct")
        .WithSummary("Actualiza los atributos, medidas, precios y estado de un producto");

        // Alternar Estado Activo / Inactivo rápidamente
        group.MapPatch("/admin/products/{id:guid}/toggle-status", async (
            Guid id,
            ICatalogRepository catalogRepo,
            CancellationToken ct) =>
        {
            var product = await catalogRepo.GetProductByIdAsync(id, ct);
            if (product == null)
            {
                return Results.NotFound(new { Message = "Producto no encontrado." });
            }

            product.IsActive = !product.IsActive;
            await catalogRepo.UpdateProductAsync(product, ct);
            return Results.Ok(new { product.Id, product.IsActive });
        })
        .WithName("ToggleProductStatus")
        .WithSummary("Activa o desactiva un producto rápidamente");

        // Eliminar Producto
        group.MapDelete("/admin/products/{id:guid}", async (
            Guid id,
            ICatalogRepository catalogRepo,
            CancellationToken ct) =>
        {
            var deleted = await catalogRepo.DeleteProductAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteProduct")
        .WithSummary("Elimina un producto del catálogo");

        // Subida de Imagen de Producto
        group.MapPost("/admin/products/{id:guid}/image", async (
            Guid id,
            IFormFile file,
            IWebHostEnvironment env,
            ICatalogRepository catalogRepo,
            CancellationToken ct) =>
        {
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new { Message = "No se envió ningún archivo de imagen válido." });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(ext))
            {
                return Results.BadRequest(new { Message = "Formato no permitido. Utilice imágenes JPG, PNG o WEBP." });
            }

            var uploadsFolder = Path.Combine(env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var fileName = $"{id}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream, ct);
            }

            var relativeUrl = $"/uploads/products/{fileName}?v={DateTime.UtcNow.Ticks}";
            var success = await catalogRepo.UpdateProductImageAsync(id, relativeUrl, ct);

            if (!success)
            {
                return Results.NotFound(new { Message = "Producto no encontrado." });
            }

            return Results.Ok(new { ImageUrl = relativeUrl });
        })
        .DisableAntiforgery()
        .WithName("UploadProductImage")
        .WithSummary("Sube una foto real para el producto y actualiza su ImageUrl");

        // Importación de Artículos Gasbel desde CSV
        group.MapPost("/admin/import-csv", async (
            GasbelDbContext dbContext,
            IWebHostEnvironment env,
            CancellationToken ct) =>
        {
            // Busca data/articulos_gasbel.csv
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "articulos_gasbel.csv");
            if (!File.Exists(csvPath))
            {
                csvPath = Path.Combine(AppContext.BaseDirectory, "data", "articulos_gasbel.csv");
            }

            if (!File.Exists(csvPath))
            {
                return Results.BadRequest(new { Message = $"No se encontró el archivo CSV en: {csvPath}" });
            }

            var count = await GasbelCsvImporter.ImportArticlesFromCsvAsync(dbContext, csvPath);
            return Results.Ok(new { Message = $"Se importaron y sincronizaron exitosamente {count} artículos con sus medidas y especificaciones.", Count = count });
        })
        .WithName("ImportCsvArticles")
        .WithSummary("Importa y sincroniza la base de artículos original del cliente");

        // ==========================================
        // CONFIGURACIÓN COMERCIAL Y CLIENTES ESPECIALES
        // ==========================================

        // Obtener configuración comercial pública (monto envío gratis, monto mínimo descuento, etc.)
        group.MapGet("/settings", async (ICommercialSettingsService settingsService, CancellationToken ct) =>
        {
            var settings = await settingsService.GetSettingsAsync(ct);
            return Results.Ok(settings);
        })
        .WithName("GetCommercialSettings")
        .WithSummary("Obtiene las reglas comerciales vigentes (montos mínimos, WhatsApp, etc.)");

        // Actualizar configuración comercial (Admin)
        group.MapPut("/admin/settings", async (
            [FromBody] CommercialSettings newSettings,
            ICommercialSettingsService settingsService,
            CancellationToken ct) =>
        {
            var updated = await settingsService.UpdateSettingsAsync(newSettings, ct);
            return Results.Ok(updated);
        })
        .WithName("UpdateCommercialSettings")
        .WithSummary("Actualiza los parámetros comerciales desde el panel de administración");

        // Listado de Clientes con Descuento Especial
        group.MapGet("/special-clients", async (ICommercialSettingsService settingsService, CancellationToken ct) =>
        {
            var clients = await settingsService.GetSpecialClientsAsync(ct);
            return Results.Ok(clients);
        })
        .WithName("GetSpecialClients")
        .WithSummary("Lista de clientes con porcentajes de descuento individual acordados");

        // Crear Cliente Especial (Admin)
        group.MapPost("/admin/special-clients", async (
            [FromBody] SpecialDiscountClient req,
            ICommercialSettingsService settingsService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Name))
            {
                return Results.BadRequest(new { Message = "El nombre del cliente es obligatorio." });
            }

            var created = await settingsService.AddSpecialClientAsync(req, ct);
            return Results.Created($"/api/special-clients/{created.Id}", created);
        })
        .WithName("CreateSpecialClient")
        .WithSummary("Registra un cliente especial con porcentaje de descuento personalizado");

        // Modificar Cliente Especial (Admin)
        group.MapPut("/admin/special-clients/{id:guid}", async (
            Guid id,
            [FromBody] SpecialDiscountClient req,
            ICommercialSettingsService settingsService,
            CancellationToken ct) =>
        {
            req.Id = id;
            var updated = await settingsService.UpdateSpecialClientAsync(req, ct);
            return updated != null ? Results.Ok(updated) : Results.NotFound();
        })
        .WithName("UpdateSpecialClient")
        .WithSummary("Modifica un cliente especial existente");

        // Eliminar Cliente Especial (Admin)
        group.MapDelete("/admin/special-clients/{id:guid}", async (
            Guid id,
            ICommercialSettingsService settingsService,
            CancellationToken ct) =>
        {
            var deleted = await settingsService.DeleteSpecialClientAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteSpecialClient")
        .WithSummary("Elimina un cliente especial de la grilla");
    }

    private static ProductDto MapToProductDto(Product p, PricingResult priceRes)
    {
        var tiers = p.TierPrices
            .OrderBy(t => t.MinQuantity)
            .Select(t => new TierPriceDto(t.Id, t.MinQuantity, t.UnitPrice, t.CustomerGroupId))
            .ToList();

        return new ProductDto(
            p.Id,
            p.CategoryId,
            p.Category?.Name ?? string.Empty,
            p.Sku,
            p.SupplierCode,
            p.Name,
            p.Description,
            p.Dimensions,
            p.Capacity,
            p.PackageQuantity,
            p.Supplier,
            p.Notes,
            p.UnitOfMeasure.ToString(),
            p.PackagingType.ToString(),
            p.UnitsPerBox,
            p.CostPrice,
            p.BasePrice,
            p.WholesalePrice,
            priceRes.UnitPrice,
            priceRes.AppliedSource,
            priceRes.SavingsPercentage,
            p.StockUnits,
            p.IsActive,
            p.ImageUrl,
            tiers
        );
    }
}

public record ParseAttributesRequest(string Title, string? Supplier);
