using Gasbel.Application.Interfaces;
using Gasbel.Application.Pricing;
using Gasbel.Application.Quotes;
using Gasbel.Infrastructure.Persistence;
using Gasbel.Infrastructure.Repositories;
using Gasbel.Infrastructure.Seeders;
using Gasbel.Web.Endpoints;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

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
        options.UseSqlite(connectionString ?? "Data Source=gasbel_b2b.db");
    }
});

// Repositories
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();

// Application Services
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IQuoteService, QuoteService>();

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

// Auto-migrate and seed in development
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GasbelDbContext>();
    await db.Database.EnsureCreatedAsync();
    await GasbelDataSeeder.SeedAsync(db);
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
