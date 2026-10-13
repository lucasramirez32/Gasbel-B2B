namespace Gasbel.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public string? ImageUrl { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
