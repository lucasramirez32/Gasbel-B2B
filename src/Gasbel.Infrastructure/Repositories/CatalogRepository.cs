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
}
