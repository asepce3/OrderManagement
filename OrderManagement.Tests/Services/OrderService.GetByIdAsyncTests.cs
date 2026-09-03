namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task GetByIdAsync_WhenRoleIsAdmin_CanAccessAnotherUsersOrder()
    {
        var order = BuildOrder(userId: 5, status: OrderStatus.Pending);
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().GetByIdAsync(userId: 1, role: UserRoles.Admin, id: order.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.Code);
        Assert.NotNull(result.Data);
        Assert.Equal(order.Id, result.Data!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleIsUserAndOrderBelongsToCaller_ReturnsOrder()
    {
        const int ownerUserId = 7;
        var order = BuildOrder(userId: ownerUserId, status: OrderStatus.Pending);
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().GetByIdAsync(userId: ownerUserId, role: UserRoles.User, id: order.Id);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(order.Id, result.Data!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WhenRoleIsUserAndOrderBelongsToSomeoneElse_ReturnsNotFound()
    {
        var order = BuildOrder(userId: 5);
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().GetByIdAsync(userId: 1, role: UserRoles.User, id: order.Id);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Equal("Order not found.", result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(orderId))
            .ReturnsAsync((Order?)null);

        var result = await CreateService().GetByIdAsync(1, UserRoles.Admin, orderId);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Equal("Order not found.", result.Error);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsMappedDto()
    {
        var productId = Guid.NewGuid();
        var detail = BuildOrderDetail(productId, qty: 3, price: 25m);
        var order = BuildOrder(
            userId: 3,
            status: OrderStatus.Shipped,
            totalPrice: 75m,
            details: new List<OrderDetail> { detail });
        _orderRepositoryMock
            .Setup(r => r.GetByIdAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().GetByIdAsync(3, UserRoles.User, order.Id);

        Assert.True(result.IsSuccess);
        var dto = result.Data;
        Assert.NotNull(dto);
        Assert.Equal(order.Id, dto!.Id);
        Assert.Equal(order.Status, dto.Status);
        Assert.Equal(order.TotalPrice, dto.TotalPrice);
        Assert.Equal(3, dto.UserId);
        Assert.Equal(order.CreatedAt, dto.CreatedAt);

        var item = Assert.Single(dto.Items);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal(3, item.Qty);
        Assert.Equal(25m, item.Price);
        Assert.Equal(75m, item.SubTotal);
    }
}
