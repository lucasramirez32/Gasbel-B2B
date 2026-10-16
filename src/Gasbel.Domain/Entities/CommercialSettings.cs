namespace Gasbel.Domain.Entities;

public class CommercialSettings
{
    public decimal MinPurchaseForDiscount { get; set; } = 50000m;
    public decimal MinPurchaseForFreeShipping { get; set; } = 100000m;
    public string WhatsAppProduction { get; set; } = "2616799084";
    public string WhatsAppTesting { get; set; } = "2613890532";
    public bool UseTestingNumber { get; set; } = true;
    public string FreeShippingLegendPrefix { get; set; } = "El envío es gratis a partir de ";
}
