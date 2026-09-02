using OrderManagement.DTOs.Queries;
using OrderManagement.Models;

namespace OrderManagement.Repositories;

public interface IOrderRepository
{
    Task<IEnumerable<Order>> GetAllAsync(GetOrdersQueryFilter filter);
    Task<Order?> GetByIdAsync(Guid id);
    Task<Order?> GetByIdForUpdateAsync(Guid id);
    Task<Order> CreateAsync(Order order);
    Task UpdateAsync(Order order);
    Task<bool> ExistsAsync(Guid id);
}

public interface IOrderDetailRepository
{
    Task<IEnumerable<OrderDetail>> GetByOrderIdAsync(Guid orderId);
    Task<OrderDetail> CreateAsync(OrderDetail detail);
}
