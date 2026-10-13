using Gasbel.Application.Pricing;
using Gasbel.Domain.Entities;
using Gasbel.Domain.Enums;
using Xunit;

namespace Gasbel.Tests;

public class PricingServiceTests
{
    private readonly PricingService _pricingService = new();

    private Product CreateSampleProduct()
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "BOL-CAM-4050",
            Name = "Bolsa Camiseta 40x50",
            BasePrice = 3200.00m,
            UnitsPerBox = 20
        };

        var gastroGroup = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        // Group price: $2800
        product.GroupPrices.Add(new GroupPrice
        {
            ProductId = product.Id,
            CustomerGroupId = gastroGroup,
            UnitPrice = 2800.00m
        });

        // Customer price: $2700
        product.CustomerPrices.Add(new CustomerPrice
        {
            ProductId = product.Id,
            CustomerId = customerId,
            UnitPrice = 2700.00m
        });

        // Tier price: >= 20 units -> $2500, >= 100 units -> $2200
        product.TierPrices.Add(new TierPrice
        {
            ProductId = product.Id,
            MinQuantity = 20,
            UnitPrice = 2500.00m
        });

        product.TierPrices.Add(new TierPrice
        {
            ProductId = product.Id,
            MinQuantity = 100,
            UnitPrice = 2200.00m
        });

        return product;
    }

    [Fact]
    public void Level4_WhenAnonymousAndSmallQuantity_ReturnsBasePrice()
    {
        var product = CreateSampleProduct();

        var result = _pricingService.CalculatePrice(product, quantity: 5);

        Assert.Equal(3200.00m, result.UnitPrice);
        Assert.Equal(16000.00m, result.Subtotal);
        Assert.Equal("BasePrice", result.AppliedSource);
        Assert.Equal(0m, result.SavingsPercentage);
        Assert.False(result.IsFullBox);
        Assert.Equal(20, result.NextTierQuantity);
        Assert.Equal(15, result.UnitsNeededForNextTier);
    }

    [Fact]
    public void Level3_WhenCustomerBelongsToGroup_ReturnsGroupPrice()
    {
        var product = CreateSampleProduct();
        var gastroGroupId = product.GroupPrices.First().CustomerGroupId;

        var result = _pricingService.CalculatePrice(product, quantity: 5, customerGroupId: gastroGroupId);

        Assert.Equal(2800.00m, result.UnitPrice);
        Assert.Equal(14000.00m, result.Subtotal);
        Assert.Equal("GroupPrice", result.AppliedSource);
        Assert.True(result.SavingsPercentage > 0);
    }

    [Fact]
    public void Level2_WhenCustomerHasCustomAgreement_ReturnsCustomerPrice()
    {
        var product = CreateSampleProduct();
        var gastroGroupId = product.GroupPrices.First().CustomerGroupId;
        var customerId = product.CustomerPrices.First().CustomerId;

        // Has both group and individual price; individual should take precedence
        var result = _pricingService.CalculatePrice(product, quantity: 5, customerId: customerId, customerGroupId: gastroGroupId);

        Assert.Equal(2700.00m, result.UnitPrice);
        Assert.Equal(13500.00m, result.Subtotal);
        Assert.Equal("CustomerPrice", result.AppliedSource);
    }

    [Fact]
    public void Level1_WhenQuantityReachesTierThreshold_TierPriceOverridesAll()
    {
        var product = CreateSampleProduct();
        var gastroGroupId = product.GroupPrices.First().CustomerGroupId;
        var customerId = product.CustomerPrices.First().CustomerId;

        // Buying 20 units (1 bulto cerrado); should activate Tier 1 ($2500)
        var result = _pricingService.CalculatePrice(product, quantity: 20, customerId: customerId, customerGroupId: gastroGroupId);

        Assert.Equal(2500.00m, result.UnitPrice);
        Assert.Equal(50000.00m, result.Subtotal);
        Assert.Equal("TierPrice", result.AppliedSource);
        Assert.True(result.IsFullBox);
        Assert.Equal(100, result.NextTierQuantity);
        Assert.Equal(80, result.UnitsNeededForNextTier);
    }

    [Fact]
    public void Level1_WhenQuantityReachesTopTier_AppliesMaxDiscount()
    {
        var product = CreateSampleProduct();

        var result = _pricingService.CalculatePrice(product, quantity: 120);

        Assert.Equal(2200.00m, result.UnitPrice);
        Assert.Equal(264000.00m, result.Subtotal);
        Assert.Equal("TierPrice", result.AppliedSource);
        Assert.True(result.IsFullBox);
        Assert.Null(result.NextTierQuantity); // No higher tier
        Assert.Null(result.UnitsNeededForNextTier);
    }
}
