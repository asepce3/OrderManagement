namespace OrderManagement.Models;

public static class OrderStatus
{
    public const string Pending = "Pending";
    public const string Confirmed = "Confirmed";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
}

public class Order
{
    public Guid Id { get; set; }
    public string Status { get; set; } = OrderStatus.Pending;
    public decimal TotalPrice { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public int UpdatedBy { get; set; }

    public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}

public class OrderDetail
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Qty { get; set; }
    public decimal Price { get; set; }
    public decimal SubTotal => Qty * Price;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
