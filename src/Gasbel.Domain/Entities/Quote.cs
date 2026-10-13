namespace Gasbel.Domain.Entities;

public class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Draft"; // Draft, Submitted, Confirmed, Cancelled
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
