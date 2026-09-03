namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task GetAllAsync_WhenRoleIsAdmin_UsesUserIdFromRequest()
    {
        const int callerUserId = 1;
        const int requestedUserId = 42;
        var request = BuildGetOrdersRequest(userId: requestedUserId);

        _orderRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<GetOrdersQueryFilter>()))
            .ReturnsAsync(new List<Order>());

        var result = await CreateService().GetAllAsync(callerUserId, UserRoles.Admin, request);

        Assert.True(result.IsSuccess);
        _orderRepositoryMock.Verify(r => r.GetAllAsync(It.Is<GetOrdersQueryFilter>(
            f => f.UserId == requestedUserId)), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_WhenRoleIsRegularUser_ForcesCallerUserId()
    {
        const int callerUserId = 7;
        const int requestedUserId = 99;
        var request = BuildGetOrdersRequest(userId: requestedUserId);

        _orderRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<GetOrdersQueryFilter>()))
            .ReturnsAsync(new List<Order>());

        var result = await CreateService().GetAllAsync(callerUserId, UserRoles.User, request);

        Assert.True(result.IsSuccess);
        _orderRepositoryMock.Verify(r => r.GetAllAsync(It.Is<GetOrdersQueryFilter>(
            f => f.UserId == callerUserId)), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ForwardsAllRequestValuesIntoRepositoryFilter()
    {
        var cursor = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);
        var startDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var request = BuildGetOrdersRequest(
            userId: 42,
            status: OrderStatus.Pending,
            cursor: cursor,
            pageSize: 5,
            startDate: startDate,
            endDate: endDate);

        GetOrdersQueryFilter? capturedFilter = null;
        _orderRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<GetOrdersQueryFilter>()))
            .Callback<GetOrdersQueryFilter>(f => capturedFilter = f)
            .ReturnsAsync(new List<Order>());

        var result = await CreateService().GetAllAsync(1, UserRoles.Admin, request);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedFilter);
        Assert.Equal(42, capturedFilter!.UserId);
        Assert.Equal(OrderStatus.Pending, capturedFilter.Status);
        Assert.Equal(cursor, capturedFilter.Cursor);
        Assert.Equal(5, capturedFilter.PageSize);
        Assert.Equal(startDate, capturedFilter.StartDate);
        Assert.Equal(endDate, capturedFilter.EndDate);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsSuccessWithMappedDtos()
    {
        var productId = Guid.NewGuid();
        var detail = BuildOrderDetail(productId, qty: 2, price: 10m);
        var order = BuildOrder(
            userId: 5,
            status: OrderStatus.Confirmed,
            totalPrice: 20m,
            details: new List<OrderDetail> { detail });

        _orderRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<GetOrdersQueryFilter>()))
            .ReturnsAsync(new List<Order> { order });

        var result = await CreateService().GetAllAsync(5, UserRoles.Admin, BuildGetOrdersRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.Code);
        Assert.NotNull(result.Data);

        var dto = Assert.Single(result.Data!.ToList());
        Assert.Equal(order.Id, dto.Id);
        Assert.Equal(order.Status, dto.Status);
        Assert.Equal(order.TotalPrice, dto.TotalPrice);
        Assert.Equal(order.UserId, dto.UserId);
        Assert.Equal(order.CreatedAt, dto.CreatedAt);

        var itemDto = Assert.Single(dto.Items);
        Assert.Equal(productId, itemDto.ProductId);
        Assert.Equal(2, itemDto.Qty);
        Assert.Equal(10m, itemDto.Price);
        Assert.Equal(20m, itemDto.SubTotal);
    }

    [Fact]
    public async Task GetAllAsync_WhenNoOrdersExist_ReturnsEmptySuccess()
    {
        _orderRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<GetOrdersQueryFilter>()))
            .ReturnsAsync(new List<Order>());

        var result = await CreateService().GetAllAsync(1, UserRoles.Admin, BuildGetOrdersRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data!);
    }
}
