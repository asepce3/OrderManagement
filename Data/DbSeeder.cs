using Microsoft.EntityFrameworkCore;
using OrderManagement.Models;

namespace OrderManagement.Data;

public static class DbSeeder
{
    public static async Task SeedProductsAsync(AppDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Redmi 14C (6GB / 128GB)",
                Price = 1_399_000,
                StockQty = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Samsung Galaxy A06 (4GB / 128GB)",
                Price = 1_838_000,
                StockQty = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Infinix Hot 50 Pro (8GB / 256GB)",
                Price = 2_549_000,
                StockQty = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "OPPO Reno12 5G (12GB / 256GB)",
                Price = 5_499_000,
                StockQty = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Samsung Galaxy S25 Ultra 5G (12GB / 512GB)",
                Price = 20_499_000,
                StockQty = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
