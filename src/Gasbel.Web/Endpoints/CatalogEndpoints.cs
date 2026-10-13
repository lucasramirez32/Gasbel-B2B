using Gasbel.Application.DTOs;
using Gasbel.Application.Interfaces;
using Gasbel.Application.Pricing;
using Gasbel.Application.Quotes;
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
        .WithSummary("Obtiene las 4 familias de productos de descartables");

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

        // Products
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
                var tiers = p.TierPrices
                    .OrderBy(t => t.MinQuantity)
                    .Select(t => new TierPriceDto(t.Id, t.MinQuantity, t.UnitPrice, t.CustomerGroupId))
                    .ToList();

                return new ProductDto(
                    p.Id,
                    p.CategoryId,
                    p.Category?.Name ?? string.Empty,
                    p.Sku,
                    p.Name,
                    p.Description,
                    p.UnitOfMeasure.ToString(),
                    p.PackagingType.ToString(),
                    p.UnitsPerBox,
                    p.BasePrice,
                    priceRes.UnitPrice,
                    priceRes.AppliedSource,
                    priceRes.SavingsPercentage,
                    p.StockUnits,
                    p.IsActive,
                    tiers
                );
            }).ToList();

            return Results.Ok(dtos);
        })
        .WithName("GetProducts")
        .WithSummary("Lista productos con resolución de precios según contexto de cliente");

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
            var tiers = product.TierPrices
                .OrderBy(t => t.MinQuantity)
                .Select(t => new TierPriceDto(t.Id, t.MinQuantity, t.UnitPrice, t.CustomerGroupId))
                .ToList();

            var dto = new ProductDto(
                product.Id,
                product.CategoryId,
                product.Category?.Name ?? string.Empty,
                product.Sku,
                product.Name,
                product.Description,
                product.UnitOfMeasure.ToString(),
                product.PackagingType.ToString(),
                product.UnitsPerBox,
                product.BasePrice,
                priceRes.UnitPrice,
                priceRes.AppliedSource,
                priceRes.SavingsPercentage,
                product.StockUnits,
                product.IsActive,
                tiers
            );

            return Results.Ok(dto);
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
    }
}
