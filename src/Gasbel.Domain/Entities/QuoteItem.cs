namespace Gasbel.Domain.Entities;

public class QuoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public string PackagingSelected { get; set; } = "Package"; // Package o Box
    public decimal AppliedUnitPrice { get; set; }
    public string PriceSource { get; set; } = "BasePrice"; // TierPrice, CustomerPrice, GroupPrice, BasePrice
    public decimal LineSubtotal { get; set; }
}
