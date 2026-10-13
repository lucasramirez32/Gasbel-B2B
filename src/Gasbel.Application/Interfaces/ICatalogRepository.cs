using Gasbel.Domain.Entities;

namespace Gasbel.Application.Interfaces;

public interface ICatalogRepository
{
    Task<List<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<List<CustomerGroup>> GetCustomerGroupsAsync(CancellationToken cancellationToken = default);
    Task<List<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);
    Task<List<Product>> GetProductsAsync(Guid? categoryId, string? search, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Dictionary<Guid, Product>> GetProductsByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
