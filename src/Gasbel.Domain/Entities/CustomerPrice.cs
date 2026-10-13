namespace Gasbel.Domain.Entities;

public class CustomerPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public decimal UnitPrice { get; set; }
}
