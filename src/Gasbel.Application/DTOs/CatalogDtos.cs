using Gasbel.Application.Pricing;

namespace Gasbel.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string Description,
    int DisplayOrder,
    int ProductCount
);

public record TierPriceDto(
    Guid Id,
    int MinQuantity,
    decimal UnitPrice,
    Guid? CustomerGroupId
);

public record ProductDto(
    Guid Id,
    Guid CategoryId,
    string CategoryName,
    string Sku,
    string Name,
    string Description,
    string UnitOfMeasure,
    string PackagingType,
    int UnitsPerBox,
    decimal BasePrice,
    decimal CurrentCalculatedPrice,
    string CurrentPriceSource,
    decimal SavingsPercentage,
    int StockUnits,
    bool IsActive,
    List<TierPriceDto> Tiers
);

public record CustomerGroupDto(
    Guid Id,
    string Code,
    string Name,
    string Description,
    decimal DefaultDiscountPercentage
);

public record CustomerDto(
    Guid Id,
    Guid CustomerGroupId,
    string CustomerGroupName,
    string BusinessName,
    string TaxId,
    string Email,
    string Phone
);

public record CalculatePriceRequest(
    Guid ProductId,
    int Quantity,
    Guid? CustomerId,
    Guid? CustomerGroupId
);

public record CreateQuoteItemRequest(
    Guid ProductId,
    int Quantity,
    string PackagingSelected
);

public record CreateQuoteRequest(
    Guid? CustomerId,
    Guid? CustomerGroupId,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string Notes,
    List<CreateQuoteItemRequest> Items
);

public record QuoteItemResponseDto(
    Guid ProductId,
    string Sku,
    string ProductName,
    int Quantity,
    string PackagingSelected,
    decimal UnitPrice,
    string PriceSource,
    decimal LineSubtotal
);

public record QuoteResponseDto(
    Guid Id,
    string CustomerName,
    string CustomerPhone,
    string CustomerEmail,
    string Status,
    DateTime CreatedAt,
    string Notes,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    List<QuoteItemResponseDto> Items
);
