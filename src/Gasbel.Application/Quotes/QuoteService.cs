using Gasbel.Application.DTOs;
using Gasbel.Application.Interfaces;
using Gasbel.Application.Pricing;
using Gasbel.Domain.Entities;

namespace Gasbel.Application.Quotes;

public interface IQuoteService
{
    Task<QuoteResponseDto> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);
    Task<QuoteResponseDto?> GetQuoteByIdAsync(Guid quoteId, CancellationToken cancellationToken = default);
}

public class QuoteService : IQuoteService
{
    private readonly IQuoteRepository _quoteRepository;
    private readonly ICatalogRepository _catalogRepository;
    private readonly IPricingService _pricingService;

    public QuoteService(
        IQuoteRepository quoteRepository,
        ICatalogRepository catalogRepository,
        IPricingService pricingService)
    {
        _quoteRepository = quoteRepository;
        _catalogRepository = catalogRepository;
        _pricingService = pricingService;
    }

    public async Task<QuoteResponseDto> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _catalogRepository.GetProductsByIdsAsync(productIds, cancellationToken);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? "Consumidor Final" : request.CustomerName,
            CustomerPhone = request.CustomerPhone ?? string.Empty,
            CustomerEmail = request.CustomerEmail ?? string.Empty,
            Notes = request.Notes ?? string.Empty,
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow
        };

        decimal calculatedSubtotal = 0m;
        var quoteItems = new List<QuoteItem>();

        foreach (var itemReq in request.Items)
        {
            if (!products.TryGetValue(itemReq.ProductId, out var product))
            {
                continue;
            }

            int finalQuantity = itemReq.Quantity <= 0 ? 1 : itemReq.Quantity;

            // Si el cliente seleccionó empaque por Bulto Cerrado, multiplicamos por UnitsPerBox si UnitsPerBox > 1
            if (string.Equals(itemReq.PackagingSelected, "Box", StringComparison.OrdinalIgnoreCase) && product.UnitsPerBox > 1)
            {
                finalQuantity = itemReq.Quantity * product.UnitsPerBox;
            }

            var pricingResult = _pricingService.CalculatePrice(
                product,
                finalQuantity,
                request.CustomerId,
                request.CustomerGroupId);

            var quoteItem = new QuoteItem
            {
                Id = Guid.NewGuid(),
                QuoteId = quote.Id,
                ProductId = product.Id,
                Quantity = finalQuantity,
                PackagingSelected = itemReq.PackagingSelected,
                AppliedUnitPrice = pricingResult.UnitPrice,
                PriceSource = pricingResult.AppliedSource,
                LineSubtotal = pricingResult.Subtotal
            };

            calculatedSubtotal += pricingResult.Subtotal;
            quoteItems.Add(quoteItem);
        }

        quote.Items = quoteItems;
        quote.Subtotal = Math.Round(calculatedSubtotal, 2);
        quote.TaxAmount = Math.Round(calculatedSubtotal * 0.21m, 2); // 21% IVA estándar
        quote.Total = Math.Round(quote.Subtotal + quote.TaxAmount, 2);

        await _quoteRepository.CreateQuoteAsync(quote, cancellationToken);

        return MapToDto(quote, products);
    }

    public async Task<QuoteResponseDto?> GetQuoteByIdAsync(Guid quoteId, CancellationToken cancellationToken = default)
    {
        var quote = await _quoteRepository.GetQuoteByIdAsync(quoteId, cancellationToken);
        if (quote == null)
        {
            return null;
        }

        var productMap = quote.Items.ToDictionary(i => i.ProductId, i => i.Product);
        return MapToDto(quote, productMap);
    }

    private static QuoteResponseDto MapToDto(Quote quote, Dictionary<Guid, Product> products)
    {
        var itemDtos = quote.Items.Select(i =>
        {
            products.TryGetValue(i.ProductId, out var p);
            return new QuoteItemResponseDto(
                ProductId: i.ProductId,
                Sku: p?.Sku ?? "N/A",
                ProductName: p?.Name ?? "Producto Desconocido",
                Quantity: i.Quantity,
                PackagingSelected: i.PackagingSelected,
                UnitPrice: i.AppliedUnitPrice,
                PriceSource: i.PriceSource,
                LineSubtotal: i.LineSubtotal
            );
        }).ToList();

        return new QuoteResponseDto(
            Id: quote.Id,
            CustomerName: quote.CustomerName,
            CustomerPhone: quote.CustomerPhone,
            CustomerEmail: quote.CustomerEmail,
            Status: quote.Status,
            CreatedAt: quote.CreatedAt,
            Notes: quote.Notes,
            Subtotal: quote.Subtotal,
            TaxAmount: quote.TaxAmount,
            Total: quote.Total,
            Items: itemDtos
        );
    }
}
