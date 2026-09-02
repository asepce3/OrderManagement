using OrderManagement.Common;
using OrderManagement.DTOs;

namespace OrderManagement.Services;

public interface IOrderService
{
    Task<Result<IEnumerable<OrderResponseDto>>> GetAllAsync(int userId, string role, GetOrdersRequestDto request);
    Task<Result<OrderResponseDto>> GetByIdAsync(int userId, string role, Guid id);
    Task<Result<OrderResponseDto>> CreateAsync(CreateOrderDto dto, int userId);
    Task<Result<OrderResponseDto>> UpdateStatusAsync(Guid id, UpdateOrderDto dto, int updatedBy, string role);
}
