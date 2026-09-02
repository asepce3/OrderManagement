using OrderManagement.Models;

namespace OrderManagement.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetAllAsync();
    Task<Product?> GetByIdAsync(Guid id);
    Task<Product?> GetByIdForUpdateAsync(Guid id);
    Task<List<Product>> GetManyByIdForUpdateAsync(Guid[] ids);
    Task<Product> CreateAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
}
