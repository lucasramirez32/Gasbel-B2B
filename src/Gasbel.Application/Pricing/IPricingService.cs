using Gasbel.Domain.Entities;

namespace Gasbel.Application.Pricing;

public record PricingResult(
    decimal UnitPrice,
    decimal Subtotal,
    string AppliedSource, // "TierPrice", "CustomerPrice", "GroupPrice", "BasePrice"
    int? NextTierQuantity,
    decimal? NextTierUnitPrice,
    int? UnitsNeededForNextTier,
    decimal SavingsPercentage,
    int UnitsPerBox,
    bool IsFullBox
);

public interface IPricingService
{
    PricingResult CalculatePrice(
        Product product,
        int quantity,
        Guid? customerId = null,
        Guid? customerGroupId = null);
}
