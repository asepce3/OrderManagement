using OrderManagement.DTOs;
using OrderManagement.Models;

namespace OrderManagement.Mappings;

public static class OrderMapper
{
    public static OrderDetail ToEntity(this CreateOrderItemDto dto, decimal price)
    {
        return new OrderDetail
        {
            Id = Guid.NewGuid(),
            ProductId = dto.ProductId,
            Qty = dto.Qty,
            Price = price,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Order ToOrderEntity(this CreateOrderDto dto, int userId, decimal totalPrice, List<OrderDetail> details)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            Status = OrderStatus.Pending,
            TotalPrice = totalPrice,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        foreach (var detail in details)
        {
            detail.OrderId = order.Id;
            order.OrderDetails.Add(detail);
        }

        return order;
    }

    public static void UpdateEntity(this UpdateOrderDto dto, Order order, int updatedBy)
    {
        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;
        order.UpdatedBy = updatedBy;
    }

    public static OrderResponseDto ToResponseDto(this Order order)
    {
        return new OrderResponseDto
        {
            Id = order.Id,
            Status = order.Status,
            TotalPrice = order.TotalPrice,
            UserId = order.UserId,
            CreatedAt = order.CreatedAt,
            Items = order.OrderDetails.Select(d => d.ToResponseDto()).ToList()
        };
    }

    public static OrderDetailResponseDto ToResponseDto(this OrderDetail detail)
    {
        return new OrderDetailResponseDto
        {
            Id = detail.Id,
            ProductId = detail.ProductId,
            ProductName = detail.Product?.Name ?? string.Empty,
            Qty = detail.Qty,
            Price = detail.Price
        };
    }
}
