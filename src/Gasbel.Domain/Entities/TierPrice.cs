namespace Gasbel.Domain.Entities;

public class TierPrice
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>
    /// Si es null, aplica a cualquier cliente. Si tiene valor, aplica específicamente a ese grupo.
    /// </summary>
    public Guid? CustomerGroupId { get; set; }
    public CustomerGroup? CustomerGroup { get; set; }

    /// <summary>
    /// Cantidad mínima de unidades requeridas para alcanzar esta escala de precio.
    /// </summary>
    public int MinQuantity { get; set; }

    /// <summary>
    /// Precio unitario con descuento a partir de la cantidad mínima.
    /// </summary>
    public decimal UnitPrice { get; set; }
}
