namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _orderRepositoryMock = new();
    private readonly Mock<IProductRepository> _productRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ILogger<OrderService> _logger = NullLogger<OrderService>.Instance;

    private OrderService CreateService()
        => new(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _logger);

    private static Product BuildProduct(
        Guid id,
        string name = "Product",
        int stockQty = 100,
        decimal price = 50m)
        => new()
        {
            Id = id,
            Name = name,
            StockQty = stockQty,
            Price = price,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

    private static OrderDetail BuildOrderDetail(
        Guid productId,
        int qty,
        decimal price,
        Guid? id = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            ProductId = productId,
            Qty = qty,
            Price = price,
            CreatedAt = DateTime.UtcNow,
        };

    private static Order BuildOrder(
        Guid? id = null,
        int userId = 1,
        string status = OrderStatus.Pending,
        decimal totalPrice = 0m,
        List<OrderDetail>? details = null)
        => new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId,
            Status = status,
            TotalPrice = totalPrice,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId,
            OrderDetails = details ?? new List<OrderDetail>(),
        };

    private static CreateOrderDto BuildCreateOrderDto(params (Guid ProductId, int Qty)[] items)
        => new()
        {
            Items = items
                .Select(i => new CreateOrderItemDto { ProductId = i.ProductId, Qty = i.Qty })
                .ToList(),
        };

    private static GetOrdersRequestDto BuildGetOrdersRequest(
        int? userId = null,
        string? status = null,
        DateTime? cursor = null,
        int? pageSize = 20,
        DateTime? startDate = null,
        DateTime? endDate = null)
        => new()
        {
            UserId = userId,
            Status = status,
            Cursor = cursor,
            PageSize = pageSize,
            StartDate = startDate,
            EndDate = endDate,
        };

    private static UpdateOrderDto BuildUpdateOrderDto(string status)
        => new() { Status = status };
}
