using OrderManagement.DTOs;
using OrderManagement.Repositories;
using OrderManagement.Mappings;

namespace OrderManagement.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    }

    public async Task<IEnumerable<OrderResponseDto>> GetAllAsync()
    {
        var orders = await _orderRepository.GetAllAsync();
        return orders.Select(o => o.ToResponseDto());
    }

    public async Task<OrderResponseDto?> GetByIdAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        return order?.ToResponseDto();
    }

    public async Task<OrderResponseDto> CreateAsync(CreateOrderDto dto)
    {
        if (dto.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(dto));

        if (dto.Price < 0)
            throw new ArgumentException("Price cannot be negative.", nameof(dto));

        var order = dto.ToEntity();
        var createdOrder = await _orderRepository.CreateAsync(order);
        return createdOrder.ToResponseDto();
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateOrderDto dto)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return false;

        dto.UpdateEntity(order);
        await _orderRepository.UpdateAsync(order);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order is null)
            return false;

        await _orderRepository.DeleteAsync(id);
        return true;
    }
}
