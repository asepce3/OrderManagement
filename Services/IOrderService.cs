using OrderManagement.DTOs;

namespace OrderManagement.Services;

public interface IOrderService
{
    Task<IEnumerable<OrderResponseDto>> GetAllAsync();
    Task<OrderResponseDto?> GetByIdAsync(Guid id);
    Task<OrderResponseDto> CreateAsync(CreateOrderDto dto);
    Task<bool> UpdateAsync(Guid id, UpdateOrderDto dto);
    Task<bool> DeleteAsync(Guid id);
}
