using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.Models;

namespace OrderManagement.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Product>> GetAllAsync()
    {
        return await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Product?> GetByIdAsync(Guid id)
    {
        return await _context.Products.FindAsync(id);
    }

    public async Task<Product?> GetByIdForUpdateAsync(Guid id)
    {
        return await _context.Products
            .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Product>> GetManyByIdForUpdateAsync(Guid[] ids)
    {
        return await _context.Products
            .FromSqlRaw("SELECT * FROM \"Products\" WHERE \"Id\" = ANY({0}) FOR UPDATE", ids)
            .ToListAsync();
    }

    public async Task<Product> CreateAsync(Product product)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return product;
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is not null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Products.AnyAsync(p => p.Id == id);
    }
}
