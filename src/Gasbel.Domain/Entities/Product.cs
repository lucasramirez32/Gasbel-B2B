using Gasbel.Domain.Enums;

namespace Gasbel.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Package;
    public PackagingType PackagingType { get; set; } = PackagingType.Box;
    
    /// <summary>
    /// Cantidad de unidades/paquetes base contenidas en un bulto cerrado o caja.
    /// </summary>
    public int UnitsPerBox { get; set; } = 1;

    /// <summary>
    /// Precio base de lista (minorista) por unidad de medida base.
    /// </summary>
    public decimal BasePrice { get; set; }

    public int StockUnits { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }

    public ICollection<TierPrice> TierPrices { get; set; } = new List<TierPrice>();
    public ICollection<GroupPrice> GroupPrices { get; set; } = new List<GroupPrice>();
    public ICollection<CustomerPrice> CustomerPrices { get; set; } = new List<CustomerPrice>();
}
