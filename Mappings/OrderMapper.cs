using OrderManagement.DTOs;
using OrderManagement.Models;

namespace OrderManagement.Mappings;

public static class OrderMapper
{
    public static Order ToEntity(this CreateOrderDto dto)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = dto.CustomerName,
            ProductName = dto.ProductName,
            Quantity = dto.Quantity,
            Price = dto.Price,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static void UpdateEntity(this UpdateOrderDto dto, Order order)
    {
        order.CustomerName = dto.CustomerName;
        order.ProductName = dto.ProductName;
        order.Quantity = dto.Quantity;
        order.Price = dto.Price;
    }

    public static OrderResponseDto ToResponseDto(this Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            ProductName = order.ProductName,
            Quantity = order.Quantity,
            Price = order.Price,
            CreatedAt = order.CreatedAt
        };
    }
}
