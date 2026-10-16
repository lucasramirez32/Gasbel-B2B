using Gasbel.Application.Interfaces;
using Gasbel.Domain.Entities;
using Gasbel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasbel.Infrastructure.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly GasbelDbContext _dbContext;

    public CatalogRepository(GasbelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CustomerGroup>> GetCustomerGroupsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerGroups
            .OrderBy(g => g.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .Include(c => c.CustomerGroup)
            .Where(c => c.IsActive)
            .OrderBy(c => c.BusinessName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Product>> GetProductsAsync(Guid? categoryId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.TierPrices)
            .Include(p => p.GroupPrices)
            .Include(p => p.CustomerPrices)
            .Where(p => p.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term) || p.Description.ToLower().Contains(term));
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.TierPrices)
            .Include(p => p.GroupPrices)
            .Include(p => p.CustomerPrices)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Dictionary<Guid, Product>> GetProductsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.TierPrices)
            .Include(p => p.GroupPrices)
            .Include(p => p.CustomerPrices)
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    public async Task<List<Product>> GetAdminProductsAsync(string? search, Guid? categoryId, string? filterType, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .Include(p => p.Category)
            .Include(p => p.TierPrices)
            .Include(p => p.GroupPrices)
            .Include(p => p.CustomerPrices)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) ||
                                     p.Sku.ToLower().Contains(term) ||
                                     (p.SupplierCode != null && p.SupplierCode.ToLower().Contains(term)) ||
                                     (p.Supplier != null && p.Supplier.ToLower().Contains(term)) ||
                                     p.Description.ToLower().Contains(term));
        }

        // Filtros especiales
        if (!string.IsNullOrWhiteSpace(filterType))
        {
            switch (filterType.ToLower())
            {
                case "active":
                    query = query.Where(p => p.IsActive);
                    break;
                case "inactive":
                    query = query.Where(p => !p.IsActive);
                    break;
                case "missingimage":
                    query = query.Where(p => string.IsNullOrWhiteSpace(p.ImageUrl));
                    break;
                case "missingdimensions":
                    query = query.Where(p => string.IsNullOrWhiteSpace(p.Dimensions));
                    break;
                case "perfumery":
                    query = query.Where(p => p.Category.Slug == "perfumeria-y-aromatizacion" ||
                                             (p.Supplier != null && p.Supplier.ToLower().Contains("sandra marzan")));
                    break;
            }
        }

        return await query.OrderBy(p => p.Name).ToListAsync(cancellationToken);
    }

    public async Task<Gasbel.Application.DTOs.CatalogStatsDto> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var total = await _dbContext.Products.CountAsync(cancellationToken);
        var active = await _dbContext.Products.CountAsync(p => p.IsActive, cancellationToken);
        var inactive = total - active;
        var missingImages = await _dbContext.Products.CountAsync(p => string.IsNullOrWhiteSpace(p.ImageUrl), cancellationToken);
        var missingDimensions = await _dbContext.Products.CountAsync(p => string.IsNullOrWhiteSpace(p.Dimensions), cancellationToken);
        var perfumery = await _dbContext.Products.CountAsync(p => p.Category.Slug == "perfumeria-y-aromatizacion" ||
                                                                  (p.Supplier != null && p.Supplier.ToLower().Contains("sandra marzan")), cancellationToken);

        return new Gasbel.Application.DTOs.CatalogStatsDto(
            TotalProducts: total,
            ActiveProducts: active,
            InactiveProducts: inactive,
            MissingImages: missingImages,
            MissingDimensions: missingDimensions,
            PerfumeryCount: perfumery
        );
    }

    public async Task<Product> AddProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        _dbContext.Products.Update(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<bool> DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync([id], cancellationToken);
        if (product == null) return false;

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateProductImageAsync(Guid id, string imageUrl, CancellationToken cancellationToken = default)
    {
        var product = await _dbContext.Products.FindAsync([id], cancellationToken);
        if (product == null) return false;

        product.ImageUrl = imageUrl;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
