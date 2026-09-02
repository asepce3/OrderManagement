namespace OrderManagement.DTOs;

public class OrderResponseDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderDetailResponseDto> Items { get; set; } = new();
}
