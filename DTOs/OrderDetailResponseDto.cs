namespace OrderManagement.DTOs;

public class OrderDetailResponseDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public decimal SubTotal => Qty * Price;
}
