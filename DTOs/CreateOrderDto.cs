namespace OrderManagement.DTOs;

public class CreateOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Qty { get; set; }
}

public class CreateOrderDto
{
    public List<CreateOrderItemDto> Items { get; set; } = new();
}
