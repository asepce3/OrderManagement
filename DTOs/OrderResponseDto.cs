namespace OrderManagement.DTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalAmount => Quantity * Price;
    public DateTime CreatedAt { get; set; }
}
