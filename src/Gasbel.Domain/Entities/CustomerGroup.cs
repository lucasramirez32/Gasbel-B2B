using Gasbel.Domain.Enums;

namespace Gasbel.Domain.Entities;

public class CustomerGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public CustomerGroupCode Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DefaultDiscountPercentage { get; set; } = 0m;

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<GroupPrice> GroupPrices { get; set; } = new List<GroupPrice>();
    public ICollection<TierPrice> TierPrices { get; set; } = new List<TierPrice>();
}
