using Gasbel.Application.Services;
using Xunit;

namespace Gasbel.Tests;

public class ProductSpecificationParserTests
{
    [Theory]
    [InlineData("ARRANQUE COMUN 20X30", "20X30 cm", null, null)]
    [InlineData("AGITADOR MADERA 15 CM X 100 U", "15 CM", null, "X 100 U")]
    [InlineData("AROMATIZADOR LBLUE 500 ML TAL CUAL", null, "500 ML", null)]
    [InlineData("BANDEJA ALUMINIO F 100 16X23", "16X23 cm", null, null)]
    [InlineData("BOLSA PP 10X15X100 U", "10X15 cm", null, "X 100 U")]
    [InlineData("VASO ENPOLEX TERMICO 180CC X25", null, "180CC", "X 25")]
    [InlineData("BOLSA BOUTIQUE N°2 22X10X33", "22X10X33 cm", null, null)]
    public void Parse_ExtractsAttributesCorrectly(string title, string? expectedDim, string? expectedCap, string? expectedPack)
    {
        var result = ProductSpecificationParser.Parse(title);

        if (expectedDim != null)
        {
            Assert.Contains(expectedDim.Replace(" cm", ""), result.Dimensions ?? "");
        }

        if (expectedCap != null)
        {
            Assert.Equal(expectedCap, result.Capacity);
        }

        if (expectedPack != null)
        {
            Assert.Equal(expectedPack, result.PackageQuantity);
        }
    }

    [Theory]
    [InlineData("AROMATIZADOR LIMON MANZANILLA 120 ML", "SANDRA MARZAN", true)]
    [InlineData("PERFUME 50 ML GOOD GIRL", "SANDRA MARZAN", true)]
    [InlineData("DIFUSOR VAINILLA 120 ML", "SANDRA MARZAN", true)]
    [InlineData("VASO TERMICO 180 CC", "NOVO DESCARTABLES", false)]
    [InlineData("BANDEJA ALUMINIO 16X23", "MOYA", false)]
    public void IsPerfumery_DetectsCorrectly(string title, string supplier, bool expectedPerfumery)
    {
        var isPerfume = ProductSpecificationParser.IsPerfumery(title, supplier);
        Assert.Equal(expectedPerfumery, isPerfume);
    }

    [Theory]
    [InlineData("AGITADOR CAFE CRISTAL LARGO X 1000 U", "AGITADOR CAFE CRISTAL LARGO", "X 1000 U")]
    [InlineData("AGITADOR CAFE CRISTAL LARGO X 500 U", "AGITADOR CAFE CRISTAL LARGO", "X 500 U")]
    [InlineData("AGITADOR CAFE CRISTAL X 100 GRS", "AGITADOR CAFE CRISTAL", "X 100 GRS")]
    [InlineData("AGITADOR CAFE CRISTAL X 1000 U", "AGITADOR CAFE CRISTAL", "X 1000 U")]
    public void Parse_CleansAdditionalDataFromTitle(string title, string expectedCleanName, string expectedPack)
    {
        var result = ProductSpecificationParser.Parse(title);
        Assert.Equal(expectedCleanName, result.CleanName);
        Assert.Equal(expectedPack, result.PackageQuantity);
    }
}
