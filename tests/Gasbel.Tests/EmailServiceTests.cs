using Gasbel.Domain.Entities;
using Gasbel.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Gasbel.Tests;

public class EmailServiceTests
{
    [Fact]
    public async Task SendQuoteEmailsAsync_InSimulatedMode_GeneratesHtmlAndReportsSuccess()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<EmailService>.Instance;
        var emailService = new EmailService(httpClient, logger);

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Sku = "VASO-001",
            Name = "Vaso Plástico Descartable 200cc",
            BasePrice = 1500m,
            WholesalePrice = 1200m
        };

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerName = "Mariano Perez",
            CustomerPhone = "2614567890",
            CustomerEmail = "mariano@ejemplo.com",
            Notes = "Dirección: Belgrano 1234, Mendoza | Envío: Sí (Gratis)",
            Subtotal = 12000m,
            TaxAmount = 2520m,
            Total = 14520m,
            CreatedAt = DateTime.UtcNow,
            Items = new List<QuoteItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    Product = product,
                    Quantity = 10,
                    PackagingSelected = "Box",
                    AppliedUnitPrice = 1200m,
                    LineSubtotal = 12000m
                }
            }
        };

        var settings = new CommercialSettings
        {
            NotificationEmail = "pedidos@gasbel.com.ar",
            SenderEmail = "cotizaciones@gasbel.com.ar",
            SendEmailToBusiness = true,
            SendEmailToCustomer = true,
            WhatsAppProduction = "2616799084"
        };

        // Act
        var result = await emailService.SendQuoteEmailsAsync(quote, settings);

        // Assert
        Assert.True(result.BusinessEmailSent);
        Assert.True(result.CustomerEmailSent);
        Assert.NotNull(result.PreviewHtmlPath);
        Assert.True(File.Exists(result.PreviewHtmlPath));

        var htmlContent = await File.ReadAllTextAsync(result.PreviewHtmlPath);
        Assert.Contains("Mariano Perez", htmlContent);
        Assert.Contains("Abrir WhatsApp con el Cliente", htmlContent);
    }

    [Fact]
    public async Task SendQuoteEmailsAsync_WhenCustomerEmailIsMissing_DoesNotSendCustomerEmail()
    {
        // Arrange
        var httpClient = new HttpClient();
        var logger = NullLogger<EmailService>.Instance;
        var emailService = new EmailService(httpClient, logger);

        var quote = new Quote
        {
            Id = Guid.NewGuid(),
            CustomerName = "Juan Sin Mail",
            CustomerPhone = "2610000000",
            CustomerEmail = "", // Sin email
            Subtotal = 5000m,
            TaxAmount = 1050m,
            Total = 6050m,
            CreatedAt = DateTime.UtcNow
        };

        var settings = new CommercialSettings
        {
            NotificationEmail = "pedidos@gasbel.com.ar",
            SendEmailToBusiness = true,
            SendEmailToCustomer = true
        };

        // Act
        var result = await emailService.SendQuoteEmailsAsync(quote, settings);

        // Assert
        Assert.True(result.BusinessEmailSent);
        Assert.False(result.CustomerEmailSent);
    }
}
