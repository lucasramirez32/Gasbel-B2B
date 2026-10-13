namespace Gasbel.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CustomerGroupId { get; set; }
    public CustomerGroup CustomerGroup { get; set; } = null!;

    public string BusinessName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty; // CUIT / DNI
    public string ContactName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CustomerPrice> CustomerPrices { get; set; } = new List<CustomerPrice>();
    public ICollection<Quote> Quotes { get; set; } = new List<Quote>();
}
