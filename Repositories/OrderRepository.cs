using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;
using OrderManagement.DTOs.Queries;
using OrderManagement.Models;

namespace OrderManagement.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<Order>> GetAllAsync(GetOrdersQueryFilter filter)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product)
            .AsQueryable();

        if (filter.Cursor.HasValue)
        {
            query = query.Where(o => o.CreatedAt < filter.Cursor.Value);
        }

        if (filter.UserId != null)
        {
            query = query.Where(o => o.UserId == filter.UserId);
        }

        if (filter.Status != null)
        {
            query = query.Where(o => o.Status == filter.Status);
        }

        if (filter.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= filter.StartDate);
        }

        if (filter.EndDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= filter.EndDate);
        }

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Take(filter.PageSize ?? 20)
            .ToListAsync();
    }

    public async Task<Order?> GetByIdAsync(Guid id)
    {
        return await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderDetails)
            .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<Order?> GetByIdForUpdateAsync(Guid id)
    {
        return await _context.Orders
            .FromSqlRaw("SELECT * FROM \"Orders\" WHERE \"Id\" = {0} FOR UPDATE", id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Orders.AnyAsync(o => o.Id == id);
    }

    public async Task<Order> CreateAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        return order;
    }

    public async Task UpdateAsync(Order order)
    {
        _context.Orders.Update(order);
        await _context.SaveChangesAsync();
    }
}

public class OrderDetailRepository : IOrderDetailRepository
{
    private readonly AppDbContext _context;

    public OrderDetailRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(Guid orderId)
    {
        return await _context.OrderDetails
            .AsNoTracking()
            .Where(d => d.OrderId == orderId)
            .ToListAsync();
    }

    public async Task<OrderDetail> CreateAsync(OrderDetail detail)
    {
        _context.OrderDetails.Add(detail);
        await _context.SaveChangesAsync();
        return detail;
    }
}
