using Gasbel.Application.Services;
using Gasbel.Domain.Entities;
using Xunit;

namespace Gasbel.Tests;

public class CommercialSettingsServiceTests
{
    [Fact]
    public async Task GetSettingsAsync_ReturnsDefaultCommercialSettings()
    {
        var service = new CommercialSettingsService();
        var settings = await service.GetSettingsAsync();

        Assert.NotNull(settings);
        Assert.True(settings.MinPurchaseForDiscount >= 0);
        Assert.True(settings.MinPurchaseForFreeShipping >= 0);
        Assert.False(string.IsNullOrWhiteSpace(settings.WhatsAppProduction));
        Assert.False(string.IsNullOrWhiteSpace(settings.WhatsAppTesting));
    }

    [Fact]
    public async Task UpdateSettingsAsync_UpdatesValuesCorrectly()
    {
        var service = new CommercialSettingsService();
        var original = await service.GetSettingsAsync();

        var updated = new CommercialSettings
        {
            MinPurchaseForDiscount = 55000m,
            MinPurchaseForFreeShipping = 120000m,
            WhatsAppProduction = "2616799084",
            WhatsAppTesting = "2613890532",
            UseTestingNumber = true,
            FreeShippingLegendPrefix = "El envío es gratis a partir de "
        };

        var result = await service.UpdateSettingsAsync(updated);
        Assert.Equal(55000m, result.MinPurchaseForDiscount);
        Assert.Equal(120000m, result.MinPurchaseForFreeShipping);

        // Restore original
        await service.UpdateSettingsAsync(original);
    }

    [Fact]
    public async Task SpecialClients_Add_Update_Delete_WorksCorrectly()
    {
        var service = new CommercialSettingsService();

        var client = new SpecialDiscountClient
        {
            Name = "Cliente Test Unitario",
            DiscountPercentage = 27.5m,
            Phone = "2610001111",
            Address = "Test Address",
            Notes = "Test note",
            IsActive = true
        };

        var created = await service.AddSpecialClientAsync(client);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(27.5m, created.DiscountPercentage);

        var fetched = await service.GetSpecialClientByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Cliente Test Unitario", fetched.Name);

        fetched.DiscountPercentage = 32m;
        await service.UpdateSpecialClientAsync(fetched);

        var updated = await service.GetSpecialClientByIdAsync(created.Id);
        Assert.Equal(32m, updated!.DiscountPercentage);

        var deleted = await service.DeleteSpecialClientAsync(created.Id);
        Assert.True(deleted);

        var notFound = await service.GetSpecialClientByIdAsync(created.Id);
        Assert.Null(notFound);
    }
}
