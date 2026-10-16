using System.Text.RegularExpressions;
using Gasbel.Application.DTOs;

namespace Gasbel.Application.Services;

public static class ProductSpecificationParser
{
    // Regex para detectar packs incluso pegados a números (ej. 10X15X100 U -> extrae X 100 U)
    private static readonly Regex PackSuffixRegex = new(
        @"(?:[xX]|\bX)\s*(\d+(?:\.\d+)?)\s*(U|UN|UNID|UNIDADES|PZA)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex para packs estándar (ej. X 1000 U, X 50, X 100, X 1 U)
    private static readonly Regex PackRegex = new(
        @"(?:^|[\s_,-])[Xx]\s*(\d+(?:\.\d+)?)\s*(U|UN|UNID|UNIDADES|PZA|MTS|MTR|M|KG|GRS|G|LTS)?\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex para volúmenes / capacidades / pesos (ej. 500 ML, 180 CC, 1 LTS, 1/2 KG, 130 OZ)
    private static readonly Regex CapacityRegex = new(
        @"\b(\d+(?:[.,]\d+)?\s*(?:ML|CC|LTS|LT|OZ)\b)|(1/[248]\s*(?:KG|KGR|WORK)?\b)|(\d+(?:[.,]\d+)?\s*(?:KGS|KGR|KG|GRS|GR|G)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex para dimensiones 2D o 3D con X (ej. 15X20, 33X40, 22X10X33, 150 MTR X 18 MIC, 1.20 X 2.00 MTS)
    private static readonly Regex DimensionsRegex = new(
        @"\b(\d+(?:[.,]\d+)?\s*(?:CM|MM|MTS|MTR|M)?\s*[xX]\s*\d+(?:[.,]\d+)?(?:\s*[xX]\s*\d+(?:[.,]\d+)?)?\s*(?:CM|MM|MTS|MTR|M|MICRONES|MIC)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex para dimensiones de longitud única o diámetro (ej. 15 CM, 17 CM, 34 CM, 60 CM)
    private static readonly Regex SingleDimRegex = new(
        @"\b(\d+(?:[.,]\d+)?\s*(?:CM|MM|MTS|MTR|M))\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Regex para número de tamaño estándar (ej. N 10, N°4, N 2, N 26)
    private static readonly Regex NumberSizeRegex = new(
        @"\b(?:N°|N)\s*(\d+(?:[.,]\d+)?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static AttributeExtractionResult Parse(string rawTitle, string? supplier = null)
    {
        if (string.IsNullOrWhiteSpace(rawTitle))
        {
            return new AttributeExtractionResult(string.Empty, string.Empty, null, null, null, null);
        }

        string original = rawTitle.Trim();
        string titleWorking = original;

        string? packageQuantity = null;
        string? capacity = null;
        string? dimensions = null;

        // 1. Extraer Pack / Cantidad (primero sufijos específicos con U, luego packs genéricos)
        var packSuffixMatch = PackSuffixRegex.Match(titleWorking);
        if (packSuffixMatch.Success)
        {
            var qty = packSuffixMatch.Groups[1].Value.Trim();
            var unit = packSuffixMatch.Groups[2].Success ? $" {packSuffixMatch.Groups[2].Value.Trim().ToUpperInvariant()}" : "";
            packageQuantity = $"X {qty}{unit}".Trim();
            titleWorking = titleWorking.Remove(packSuffixMatch.Index, packSuffixMatch.Length).Trim();
        }
        else
        {
            var packMatch = PackRegex.Match(titleWorking);
            if (packMatch.Success)
            {
                var qty = packMatch.Groups[1].Value.Trim();
                var unit = packMatch.Groups[2].Success ? $" {packMatch.Groups[2].Value.Trim().ToUpperInvariant()}" : "";
                packageQuantity = $"X {qty}{unit}".Trim();
                titleWorking = titleWorking.Remove(packMatch.Index, packMatch.Length).Trim();
            }
        }

        // 2. Extraer Capacidad / Volumen / Peso
        var capMatch = CapacityRegex.Match(titleWorking);
        if (capMatch.Success)
        {
            capacity = capMatch.Value.Trim().ToUpperInvariant();
            titleWorking = titleWorking.Remove(capMatch.Index, capMatch.Length).Trim();
        }

        // 3. Extraer Dimensiones
        var dimMatch = DimensionsRegex.Match(titleWorking);
        if (dimMatch.Success)
        {
            dimensions = dimMatch.Value.Trim();
            // Si son solo números como 15X20 o 33X40, agregar 'cm'
            if (!Regex.IsMatch(dimensions, @"(?:CM|MM|MTS|MTR|M|MIC)", RegexOptions.IgnoreCase))
            {
                dimensions = $"{dimensions} cm";
            }
            titleWorking = titleWorking.Remove(dimMatch.Index, dimMatch.Length).Trim();
        }
        else
        {
            var singleDimMatch = SingleDimRegex.Match(titleWorking);
            if (singleDimMatch.Success)
            {
                dimensions = singleDimMatch.Value.Trim();
                titleWorking = titleWorking.Remove(singleDimMatch.Index, singleDimMatch.Length).Trim();
            }
        }

        // Si no hay dimensiones pero hay número de tamaño (N 2, N 10)
        if (string.IsNullOrWhiteSpace(dimensions))
        {
            var numMatch = NumberSizeRegex.Match(titleWorking);
            if (numMatch.Success)
            {
                dimensions = numMatch.Value.Trim();
            }
        }

        // 4. Limpieza profunda del nombre (CleanName)
        // Remover residuos huérfanos de "X 1 U", "X 1", "X UN", "X"
        titleWorking = Regex.Replace(titleWorking, @"\b[Xx]\s*(?:1\s*(?:U|UN|UNID)?|U|UN)\b", "", RegexOptions.IgnoreCase);
        // Remover cualquier 'X' o 'x' aislada al final del nombre
        titleWorking = Regex.Replace(titleWorking, @"\s+[xX]\s*$", "", RegexOptions.IgnoreCase);
        // Remover espacios múltiples y caracteres residuales al inicio o final
        string cleanName = Regex.Replace(titleWorking, @"\s+", " ").Trim();
        cleanName = cleanName.Trim('-', ',', '/', ' ');

        // 5. Categoría sugerida
        string suggestedCategory = DetermineCategory(original, supplier);

        return new AttributeExtractionResult(
            OriginalTitle: original,
            CleanName: string.IsNullOrWhiteSpace(cleanName) ? original : cleanName,
            Dimensions: dimensions,
            Capacity: capacity,
            PackageQuantity: packageQuantity,
            SuggestedCategory: suggestedCategory
        );
    }

    public static bool IsPerfumery(string rawTitle, string? supplier = null)
    {
        if (supplier != null && supplier.Contains("SANDRA MARZAN", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string upper = rawTitle.ToUpperInvariant();
        return upper.Contains("AROMATIZADOR") ||
               upper.Contains("AROMATIZANTE") ||
               upper.Contains("BODY SPLASH") ||
               upper.Contains("COLONIA") ||
               upper.Contains("DIFUSOR") ||
               upper.Contains("DIF COMUN") ||
               upper.Contains("HOME SPRAY") ||
               upper.Contains("PERFUME") ||
               upper.Contains("JABON ESPUMA") ||
               upper.Contains("GEL DE DUCHA") ||
               upper.Contains("BAÑO PARA BEBE") ||
               (upper.Contains("JABON LIQUIDO") && upper.Contains("SANDRA")) ||
               upper.Contains("MUJERCITAS 3 EN 1");
    }

    public static string DetermineCategory(string rawTitle, string? supplier = null)
    {
        if (IsPerfumery(rawTitle, supplier))
        {
            return "Perfumería y Aromatización";
        }

        string upper = rawTitle.ToUpperInvariant();

        if (upper.Contains("BOLSA") || upper.Contains("ARRANQUE") || upper.Contains("CAMISETA") ||
            upper.Contains("CAMI ") || upper.Contains("CONSORCIO") || upper.Contains("RESIDUO") ||
            upper.Contains("SOBRE") || upper.Contains("ZIPPER"))
        {
            return "Bolsas de Polietileno, Papel y Residuos";
        }

        if (upper.Contains("BANDEJA") || upper.Contains("BAND ") || upper.Contains("POTE") ||
            upper.Contains("CAJA") || upper.Contains("MICRO ") || upper.Contains("CUBETA") ||
            upper.Contains("MOLDE") || upper.Contains("OBLEA") || upper.Contains("ENSALADERA") ||
            upper.Contains("TORTERA") || upper.Contains("BOWL") || upper.Contains("BUDINER") ||
            upper.Contains("DOYPACK") || upper.Contains("BALDE") || upper.Contains("EXPANDIDO") ||
            upper.Contains("ESTUCHE"))
        {
            return "Envases, Bandejas Térmicas y Packaging Gastronómico";
        }

        if (upper.Contains("VASO") || upper.Contains("COPA") || upper.Contains("CUCHARA") ||
            upper.Contains("CUCHILLO") || upper.Contains("TENEDOR") || upper.Contains("PLATO") ||
            upper.Contains("SORBETE") || upper.Contains("AGITADOR") || upper.Contains("PINCHO") ||
            upper.Contains("PINCHES") || upper.Contains("ESPADITAS") || upper.Contains("BOMBILLA") ||
            upper.Contains("COLLARIN") || upper.Contains("COMPOTERA"))
        {
            return "Vasos, Copas, Cubiertos y Artículos de Mesa";
        }

        if (upper.Contains("ALUMINIO") || upper.Contains("ANTIGRASA") || upper.Contains("BOBINA") ||
            upper.Contains("BOB ") || upper.Contains("FILM") || upper.Contains("STRETCH") ||
            upper.Contains("CINTA") || upper.Contains("CINTEX") || upper.Contains("SERVILLETA") ||
            upper.Contains("SERV ") || upper.Contains("TOALLA") || upper.Contains("PAPEL") ||
            upper.Contains("PARAFINADO") || upper.Contains("PAñO") || upper.Contains("PAÑO") ||
            upper.Contains("PAÑUELO") || upper.Contains("PAñUELO") || upper.Contains("TRAPO") ||
            upper.Contains("RESMA") || upper.Contains("DISPENSER") || upper.Contains("TICKET") ||
            upper.Contains("LAMINA") || upper.Contains("SULFITO") || upper.Contains("MANTEL") ||
            upper.Contains("FONDO PARA PIZZA"))
        {
            return "Papelera Comercial, Films de Embalaje e Higiene";
        }

        return "Artículos Varios y Bazar";
    }
}
