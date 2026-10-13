using Gasbel.Domain.Entities;
using Gasbel.Domain.Enums;
using Gasbel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasbel.Infrastructure.Seeders;

public static class GasbelDataSeeder
{
    public static async Task SeedAsync(GasbelDbContext context)
    {
        if (await context.Categories.AnyAsync())
        {
            return; // Ya fue inicializado
        }

        // 1. Grupos de Clientes
        var groupRetail = new CustomerGroup
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Code = CustomerGroupCode.Retail,
            Name = "Minorista (Público General)",
            Description = "Clientes particulares y compras al por menor a precio de lista.",
            DefaultDiscountPercentage = 0m
        };

        var groupGastronomy = new CustomerGroup
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Code = CustomerGroupCode.Gastronomy,
            Name = "Comercio Gastronómico",
            Description = "Rotiserías, pizzerías, restaurantes y casas de comidas con precios preferenciales.",
            DefaultDiscountPercentage = 12m
        };

        var groupWholesale = new CustomerGroup
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Code = CustomerGroupCode.WholesaleDistributor,
            Name = "Distribuidor / Reventa",
            Description = "Papeleras locales y distribuidores con precio mayorista por bulto cerrado.",
            DefaultDiscountPercentage = 22m
        };

        var groupKeyAccount = new CustomerGroup
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Code = CustomerGroupCode.KeyAccount,
            Name = "Gran Cuenta / Corporativo",
            Description = "Cadenas, franquicias y clientes con acuerdos contractuales especiales.",
            DefaultDiscountPercentage = 28m
        };

        context.CustomerGroups.AddRange(groupRetail, groupGastronomy, groupWholesale, groupKeyAccount);

        // 2. Clientes de Ejemplo
        var customerGastro = new Customer
        {
            Id = Guid.Parse("a1111111-1111-1111-1111-111111111111"),
            CustomerGroupId = groupGastronomy.Id,
            BusinessName = "Rotisería & Pizzería Los Amigos",
            TaxId = "30-71458962-9",
            ContactName = "Carlos Gómez",
            Email = "pedidos@losamigosrotiseria.com",
            Phone = "+54 11 4455-8899",
            Address = "Av. San Martín 1420, Morón"
        };

        var customerWholesale = new Customer
        {
            Id = Guid.Parse("a2222222-2222-2222-2222-222222222222"),
            CustomerGroupId = groupWholesale.Id,
            BusinessName = "Distribuidora Mayorista PapelPack",
            TaxId = "30-68954123-4",
            ContactName = "Mariana Rossi",
            Email = "ventas@papelpack.com.ar",
            Phone = "+54 11 5566-7788",
            Address = "Ruta 8 Km 45, Pilar"
        };

        context.Customers.AddRange(customerGastro, customerWholesale);

        // 3. Familias de Productos (Categorías reales Gasbel)
        var catBolsas = new Category
        {
            Id = Guid.Parse("b1111111-1111-1111-1111-111111111111"),
            Name = "Bolsas de Polietileno, Papel y Residuos",
            Slug = "bolsas-polietileno-papel-residuos",
            Description = "Bolsas camiseta alta/baja densidad, arranque en rollo, consorcio pesadas y papel Kraft para delivery.",
            DisplayOrder = 1
        };

        var catEnvases = new Category
        {
            Id = Guid.Parse("b2222222-2222-2222-2222-222222222222"),
            Name = "Envases, Bandejas Térmicas y Packaging Gastronómico",
            Slug = "envases-bandejas-packaging-gastronomico",
            Description = "Bandejas de poliespuma (EPS), aluminio con tapa, cajas para pizza/empanadas y potes térmicos para helado.",
            DisplayOrder = 2
        };

        var catVajilla = new Category
        {
            Id = Guid.Parse("b3333333-3333-3333-3333-333333333333"),
            Name = "Vasos, Copas, Cubiertos y Artículos de Mesa",
            Slug = "vasos-copas-cubiertos-mesa",
            Description = "Vasos PP/PS, vasos térmicos de polipapel con tapa, cubiertos plásticos reforzados y platos descartables.",
            DisplayOrder = 3
        };

        var catPapelera = new Category
        {
            Id = Guid.Parse("b4444444-4444-4444-4444-444444444444"),
            Name = "Papelera Comercial, Films de Embalaje e Higiene",
            Slug = "papelera-films-embalaje-higiene",
            Description = "Film stretch virgen paletizado, film PVC gastronómico, servilletas bar/dispenser y papel institucional.",
            DisplayOrder = 4
        };

        context.Categories.AddRange(catBolsas, catEnvases, catVajilla, catPapelera);

        // 4. Productos
        var p1 = new Product
        {
            Id = Guid.Parse("c1111111-1111-1111-1111-111111111111"),
            CategoryId = catBolsas.Id,
            Sku = "BOL-CAM-4050",
            Name = "Bolsa Camiseta 40x50 Alta Densidad Blanca",
            Description = "Bolsa camiseta estándar para comercio y supermercado. Paquete x 100 unidades. Bulto cerrado x 20 paquetes (2.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 20,
            BasePrice = 3200.00m,
            StockUnits = 850
        };

        var p2 = new Product
        {
            Id = Guid.Parse("c2222222-2222-2222-2222-222222222222"),
            CategoryId = catBolsas.Id,
            Sku = "BOL-ARR-2535",
            Name = "Bolsa de Arranque en Rollo 25x35 Virgen",
            Description = "Bolsa en rollo punteado para verdulerías y fiambrerías. Rollo x 500 bolsas. Bulto cerrado x 12 rollos.",
            UnitOfMeasure = UnitOfMeasure.Roll,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 12,
            BasePrice = 4800.00m,
            StockUnits = 420
        };

        var p3 = new Product
        {
            Id = Guid.Parse("c3333333-3333-3333-3333-333333333333"),
            CategoryId = catBolsas.Id,
            Sku = "BOL-CON-80110",
            Name = "Bolsa de Consorcio Negra 80x110 Pesada",
            Description = "Bolsa para residuos extra resistente 45 micrones. Paquete x 10 unidades. Bulto cerrado x 25 paquetes (250 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 25,
            BasePrice = 3900.00m,
            StockUnits = 600
        };

        var p4 = new Product
        {
            Id = Guid.Parse("c4444444-4444-4444-4444-444444444444"),
            CategoryId = catBolsas.Id,
            Sku = "BOL-KRF-DEL",
            Name = "Bolsa Papel Kraft Fondo Cuadrado Delivery (32x22x12)",
            Description = "Bolsa ecológica Kraft reforzada para transporte de viandas y rotiserías. Paquete x 50 u. Bulto x 10 paquetes (500 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 10,
            BasePrice = 6500.00m,
            StockUnits = 310
        };

        var p5 = new Product
        {
            Id = Guid.Parse("c5555555-5555-5555-5555-555555555555"),
            CategoryId = catEnvases.Id,
            Sku = "ENV-EPS-103",
            Name = "Bandeja Poliespuma EPS Medida 103 (18x13x2cm)",
            Description = "Bandeja térmica expandida blanca para fiambres y viandas. Paquete x 100 u. Bulto x 10 paquetes (1.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 10,
            BasePrice = 2900.00m,
            StockUnits = 700
        };

        var p6 = new Product
        {
            Id = Guid.Parse("c6666666-6666-6666-6666-666666666666"),
            CategoryId = catEnvases.Id,
            Sku = "ENV-ALU-ROT",
            Name = "Bandeja Aluminio con Tapa Cartón Rotisería (1.200cc)",
            Description = "Bandeja rectangular de aluminio apta para horno y freezer. Paquete x 50 u. Bulto cerrado x 10 paquetes (500 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 10,
            BasePrice = 8200.00m,
            StockUnits = 250
        };

        var p7 = new Product
        {
            Id = Guid.Parse("c7777777-7777-7777-7777-777777777777"),
            CategoryId = catEnvases.Id,
            Sku = "CAJ-PIZ-OCT",
            Name = "Caja Microcorrugada Octogonal para Pizza 36cm",
            Description = "Caja térmica automontable blanca/marrón resistente a la grasa. Paquete x 50 u. Bulto cerrado x 4 paquetes (200 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Bundle,
            UnitsPerBox = 4,
            BasePrice = 14500.00m,
            StockUnits = 380
        };

        var p8 = new Product
        {
            Id = Guid.Parse("c8888888-8888-8888-8888-888888888888"),
            CategoryId = catEnvases.Id,
            Sku = "POT-TER-1KG",
            Name = "Pote Térmico EPS para Helado 1 Kg con Tapa",
            Description = "Pote aislante para heladerías y postres congelados. Paquete x 50 u. Bulto cerrado x 6 paquetes (300 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 6,
            BasePrice = 11800.00m,
            StockUnits = 190
        };

        var p9 = new Product
        {
            Id = Guid.Parse("c9999999-9999-9999-9999-999999999999"),
            CategoryId = catVajilla.Id,
            Sku = "VAS-PP-220",
            Name = "Vaso Descartable PP Blanco 220cc para Bebidas",
            Description = "Vaso plástico rígido descartable. Paquete x 100 u. Bulto cerrado x 20 paquetes (2.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 20,
            BasePrice = 2400.00m,
            StockUnits = 950
        };

        var p10 = new Product
        {
            Id = Guid.Parse("caaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CategoryId = catVajilla.Id,
            Sku = "VAS-POL-8OZ",
            Name = "Vaso Térmico Polipapel Café 8 oz con Tapa Viajera",
            Description = "Vaso descartable para café de especialidad y bebidas calientes con tapa viajera hermética. Paquete x 50 u. Bulto x 20 paq (1.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 20,
            BasePrice = 5200.00m,
            StockUnits = 450
        };

        var p11 = new Product
        {
            Id = Guid.Parse("cbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            CategoryId = catVajilla.Id,
            Sku = "CUB-TEN-REF",
            Name = "Tenedor Plástico Blanco Reforzado Gastronómico",
            Description = "Cubierto de alta resistencia ideal para rotiserías y eventos. Paquete x 50 u. Bulto x 20 paquetes (1.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 20,
            BasePrice = 1800.00m,
            StockUnits = 1100
        };

        var p12 = new Product
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            CategoryId = catPapelera.Id,
            Sku = "EMB-STR-VIR",
            Name = "Bobina Film Stretch Virgen Transparente 50cm (4.2 Kg)",
            Description = "Film stretch para paletizado y embalaje de mercadería con alto estiramiento. Bobina x 1u. Bulto cerrado x 4 bobinas.",
            UnitOfMeasure = UnitOfMeasure.Unit,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 4,
            BasePrice = 18500.00m,
            StockUnits = 210
        };

        var p13 = new Product
        {
            Id = Guid.Parse("cddddddd-dddd-dddd-dddd-dddddddddddd"),
            CategoryId = catPapelera.Id,
            Sku = "EMB-PVC-300",
            Name = "Bobina Film PVC Gastronómico 38cm x 300m c/Dispensador",
            Description = "Film adherente grado alimenticio con caja dispensadora y cuchilla incorporada. Bobina x 1u. Bulto cerrado x 4 unidades.",
            UnitOfMeasure = UnitOfMeasure.Unit,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 4,
            BasePrice = 12900.00m,
            StockUnits = 175
        };

        var p14 = new Product
        {
            Id = Guid.Parse("ceeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            CategoryId = catPapelera.Id,
            Sku = "PAP-SER-BAR",
            Name = "Servilletas Bar / Dispenser 14x14cm Doble Hoja",
            Description = "Servilletas de alta absorción para dispensadores de bar y mesas. Paquete x 1.000 u. Bulto cerrado x 12 paquetes (12.000 u).",
            UnitOfMeasure = UnitOfMeasure.Package,
            PackagingType = PackagingType.Box,
            UnitsPerBox = 12,
            BasePrice = 4100.00m,
            StockUnits = 530
        };

        context.Products.AddRange(p1, p2, p3, p4, p5, p6, p7, p8, p9, p10, p11, p12, p13, p14);

        // 5. Precios por Grupo (GroupPrice)
        // Ejemplo para p1 (Bolsa 40x50): Base=3200, Gastro=2800, Mayorista=2450
        context.GroupPrices.AddRange(
            new GroupPrice { ProductId = p1.Id, CustomerGroupId = groupGastronomy.Id, UnitPrice = 2800.00m },
            new GroupPrice { ProductId = p1.Id, CustomerGroupId = groupWholesale.Id, UnitPrice = 2450.00m },
            new GroupPrice { ProductId = p1.Id, CustomerGroupId = groupKeyAccount.Id, UnitPrice = 2250.00m },

            new GroupPrice { ProductId = p7.Id, CustomerGroupId = groupGastronomy.Id, UnitPrice = 12700.00m },
            new GroupPrice { ProductId = p7.Id, CustomerGroupId = groupWholesale.Id, UnitPrice = 11200.00m },

            new GroupPrice { ProductId = p12.Id, CustomerGroupId = groupWholesale.Id, UnitPrice = 14500.00m },
            new GroupPrice { ProductId = p12.Id, CustomerGroupId = groupKeyAccount.Id, UnitPrice = 13800.00m }
        );

        // 6. Escalas por Volumen / Bultos Cerrados (TierPrice)
        // Para p1: >= 20 paquetes (1 bulto) -> $2.600; >= 100 paquetes (5 bultos) -> $2.300
        context.TierPrices.AddRange(
            new TierPrice { ProductId = p1.Id, MinQuantity = 20, UnitPrice = 2600.00m },
            new TierPrice { ProductId = p1.Id, MinQuantity = 100, UnitPrice = 2300.00m },

            // Para p7 (Caja Pizza): >= 4 paquetes (1 fardo) -> $13.000; >= 20 paquetes (5 fardos) -> $11.500
            new TierPrice { ProductId = p7.Id, MinQuantity = 4, UnitPrice = 13000.00m },
            new TierPrice { ProductId = p7.Id, MinQuantity = 20, UnitPrice = 11500.00m },

            // Para p12 (Stretch): >= 4 bobinas (1 caja) -> $16.000; >= 20 bobinas -> $14.200
            new TierPrice { ProductId = p12.Id, MinQuantity = 4, UnitPrice = 16000.00m },
            new TierPrice { ProductId = p12.Id, MinQuantity = 20, UnitPrice = 14200.00m },

            // Para p14 (Servilletas): >= 12 paquetes (1 bulto) -> $3.450; >= 60 paquetes -> $3.100
            new TierPrice { ProductId = p14.Id, MinQuantity = 12, UnitPrice = 3450.00m },
            new TierPrice { ProductId = p14.Id, MinQuantity = 60, UnitPrice = 3100.00m }
        );

        // 7. Precio Contractual Individual (CustomerPrice) para Los Amigos en Cajas de Pizza y Bolsas
        context.CustomerPrices.AddRange(
            new CustomerPrice { ProductId = p7.Id, CustomerId = customerGastro.Id, UnitPrice = 12200.00m },
            new CustomerPrice { ProductId = p4.Id, CustomerId = customerGastro.Id, UnitPrice = 5400.00m }
        );

        await context.SaveChangesAsync();
    }
}
