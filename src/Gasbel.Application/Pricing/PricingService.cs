using Gasbel.Domain.Entities;

namespace Gasbel.Application.Pricing;

public class PricingService : IPricingService
{
    public PricingResult CalculatePrice(
        Product product,
        int quantity,
        Guid? customerId = null,
        Guid? customerGroupId = null)
    {
        if (quantity <= 0)
        {
            quantity = 1;
        }

        decimal appliedPrice = product.BasePrice;
        string appliedSource = "BasePrice";

        // 1. Escala por volumen (TierPrice)
        // Buscamos si califica para algún TierPrice (específico de su grupo o genérico para todos)
        var qualifyingTiers = product.TierPrices
            .Where(t => t.MinQuantity <= quantity && (t.CustomerGroupId == null || t.CustomerGroupId == customerGroupId))
            .OrderByDescending(t => t.MinQuantity)
            .ThenBy(t => t.UnitPrice)
            .ToList();

        if (qualifyingTiers.Count > 0)
        {
            appliedPrice = qualifyingTiers[0].UnitPrice;
            appliedSource = "TierPrice";
        }
        else if (customerId.HasValue && product.CustomerPrices.Any(cp => cp.CustomerId == customerId.Value))
        {
            // 2. Precio acordado individual (CustomerPrice)
            appliedPrice = product.CustomerPrices.First(cp => cp.CustomerId == customerId.Value).UnitPrice;
            appliedSource = "CustomerPrice";
        }
        else if (customerGroupId.HasValue && product.GroupPrices.Any(gp => gp.CustomerGroupId == customerGroupId.Value))
        {
            // 3. Precio por grupo de cliente (GroupPrice)
            appliedPrice = product.GroupPrices.First(gp => gp.CustomerGroupId == customerGroupId.Value).UnitPrice;
            appliedSource = "GroupPrice";
        }
        else
        {
            // 4. Precio de lista base minorista (BasePrice)
            appliedPrice = product.BasePrice;
            appliedSource = "BasePrice";
        }

        // Búsqueda del siguiente escalón de descuento (Tier futuro)
        var nextTier = product.TierPrices
            .Where(t => t.MinQuantity > quantity && (t.CustomerGroupId == null || t.CustomerGroupId == customerGroupId))
            .OrderBy(t => t.MinQuantity)
            .FirstOrDefault();

        int? nextTierQuantity = nextTier?.MinQuantity;
        decimal? nextTierUnitPrice = nextTier?.UnitPrice;
        int? unitsNeeded = nextTier != null ? nextTier.MinQuantity - quantity : null;

        decimal savingsPercentage = 0m;
        if (product.BasePrice > 0 && appliedPrice < product.BasePrice)
        {
            savingsPercentage = Math.Round(((product.BasePrice - appliedPrice) / product.BasePrice) * 100m, 1);
        }

        decimal subtotal = Math.Round(appliedPrice * quantity, 2);
        bool isFullBox = product.UnitsPerBox > 1 && quantity >= product.UnitsPerBox && (quantity % product.UnitsPerBox == 0);

        return new PricingResult(
            UnitPrice: appliedPrice,
            Subtotal: subtotal,
            AppliedSource: appliedSource,
            NextTierQuantity: nextTierQuantity,
            NextTierUnitPrice: nextTierUnitPrice,
            UnitsNeededForNextTier: unitsNeeded,
            SavingsPercentage: savingsPercentage,
            UnitsPerBox: product.UnitsPerBox,
            IsFullBox: isFullBox
        );
    }
}
