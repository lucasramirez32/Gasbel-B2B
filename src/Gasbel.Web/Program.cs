using Gasbel.Application.Interfaces;
using Gasbel.Application.Pricing;
using Gasbel.Application.Quotes;
using Gasbel.Application.Services;
using Gasbel.Infrastructure.Persistence;
using Gasbel.Infrastructure.Repositories;
using Gasbel.Infrastructure.Seeders;
using Gasbel.Infrastructure.Services;
using Gasbel.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// JSON Serialization Configuration
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Asegurar directorio persistente ./data para SQLite y configuraciones
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}

// Migrar base de datos legacy si existía en la raíz hacia ./data/gasbel_b2b.db
var legacyDbPath = Path.Combine(Directory.GetCurrentDirectory(), "gasbel_b2b.db");
var persistentDbPath = Path.Combine(dataDir, "gasbel_b2b.db");
if (File.Exists(legacyDbPath) && !File.Exists(persistentDbPath))
{
    File.Copy(legacyDbPath, persistentDbPath);
}

// Database configuration
var provider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "Sqlite";
var connectionString = provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : builder.Configuration.GetConnectionString("SqliteConnection");

builder.Services.AddDbContext<GasbelDbContext>(options =>
{
    if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseNpgsql(connectionString);
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=data/gasbel_b2b.db");
    }
});

// Repositories
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

// Application & Infrastructure Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();
builder.Services.AddSingleton<ICommercialSettingsService, CommercialSettingsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// OpenAPI Documentation
builder.Services.AddOpenApi();

var app = builder.Build();

// Asegurar carpeta para subida de fotos (volumen persistente wwwroot/uploads/products)
var uploadsDir = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "products");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
}

// Auto-migrate, seed missing articles y auto-vincular fotos desde disco
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GasbelDbContext>();
    await db.Database.EnsureCreatedAsync();
    await GasbelDataSeeder.SeedAsync(db);

    // Importar únicamente artículos faltantes desde CSV (sin pisar ediciones ni fotos existentes en DB)
    var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "articulos_gasbel.csv");
    if (File.Exists(csvPath))
    {
        await GasbelCsvImporter.ImportArticlesFromCsvAsync(db, csvPath, overwriteExisting: false);
    }

    // Auto-vincular imágenes físicas existentes en wwwroot/uploads/products/ por SKU o Id
    await GasbelCsvImporter.SyncProductImagesFromDiskAsync(db, uploadsDir);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

// Scalar / OpenAPI
app.MapOpenApi();
app.MapScalarApiReference();

// Minimal API Endpoints
app.MapCatalogEndpoints();

app.Run();
