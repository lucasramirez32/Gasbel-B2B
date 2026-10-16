using Gasbel.Domain.Enums;

namespace Gasbel.Domain.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Sku { get; set; } = string.Empty;
    public string? SupplierCode { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Medidas o dimensiones extraídas (ej. 15x20 cm, 33x40 cm, 15 cm)
    /// </summary>
    public string? Dimensions { get; set; }

    /// <summary>
    /// Capacidad o volumen extraído (ej. 500 ml, 180 cc, 1 kg)
    /// </summary>
    public string? Capacity { get; set; }

    /// <summary>
    /// Presentación o cantidad del paquete (ej. x 100 U, x 50 U, x 1 U)
    /// </summary>
    public string? PackageQuantity { get; set; }

    /// <summary>
    /// Proveedor de origen del artículo
    /// </summary>
    public string? Supplier { get; set; }

    /// <summary>
    /// Observaciones adicionales del cliente / inventario
    /// </summary>
    public string? Notes { get; set; }

    public UnitOfMeasure UnitOfMeasure { get; set; } = UnitOfMeasure.Package;
    public PackagingType PackagingType { get; set; } = PackagingType.Box;
    
    /// <summary>
    /// Cantidad de unidades/paquetes base contenidas en un bulto cerrado o caja.
    /// </summary>
    public int UnitsPerBox { get; set; } = 1;

    /// <summary>
    /// Precio de costo de compra al proveedor.
    /// </summary>
    public decimal CostPrice { get; set; }

    /// <summary>
    /// Precio venta 1: base de lista (minorista) por unidad.
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Precio venta 2: lista mayorista / gastronómico / reventa.
    /// </summary>
    public decimal WholesalePrice { get; set; }

    public int StockUnits { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }

    public ICollection<TierPrice> TierPrices { get; set; } = new List<TierPrice>();
    public ICollection<GroupPrice> GroupPrices { get; set; } = new List<GroupPrice>();
    public ICollection<CustomerPrice> CustomerPrices { get; set; } = new List<CustomerPrice>();
}
