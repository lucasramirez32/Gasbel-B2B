namespace Gasbel.Domain.Entities;

public class GroupPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid CustomerGroupId { get; set; }
    public CustomerGroup CustomerGroup { get; set; } = null!;

    public decimal UnitPrice { get; set; }
}
