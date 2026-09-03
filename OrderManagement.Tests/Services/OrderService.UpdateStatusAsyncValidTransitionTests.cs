namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Delivered)]
    public async Task UpdateStatusAsync_WhenTransitionIsValid_UpdatesEntityAndCommits(
        string currentStatus,
        string nextStatus)
    {
        const int adminUserId = 99;
        var order = BuildOrder(status: currentStatus);
        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(nextStatus),
            updatedBy: adminUserId,
            role: UserRoles.Admin);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.Code);
        Assert.Equal(nextStatus, order.Status);
        Assert.Equal(adminUserId, order.UpdatedBy);
        Assert.NotNull(result.Data);
        Assert.Equal(nextStatus, result.Data!.Status);

        _orderRepositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
        _productRepositoryMock.Verify(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenCancelling_RestoresStockForEachOrderDetail()
    {
        const int adminUserId = 99;
        var productA = BuildProduct(Guid.NewGuid(), name: "Keyboard", stockQty: 5, price: 100m);
        var productB = BuildProduct(Guid.NewGuid(), name: "Mouse", stockQty: 3, price: 50m);
        var detailA = BuildOrderDetail(productA.Id, qty: 2, price: 100m);
        var detailB = BuildOrderDetail(productB.Id, qty: 1, price: 50m);
        var order = BuildOrder(
            status: OrderStatus.Confirmed,
            totalPrice: 250m,
            details: new List<OrderDetail> { detailA, detailB });

        _orderRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(order.Id)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync(order);
        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { productA, productB });

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(OrderStatus.Cancelled),
            updatedBy: adminUserId,
            role: UserRoles.Admin);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status200OK, result.Code);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(adminUserId, order.UpdatedBy);
        Assert.NotNull(result.Data);
        Assert.Equal(OrderStatus.Cancelled, result.Data!.Status);

        Assert.Equal(7, productA.StockQty); // 5 + 2
        Assert.Equal(4, productB.StockQty); // 3 + 1

        _productRepositoryMock.Verify(r => r.UpdateAsync(productA), Times.Once);
        _productRepositoryMock.Verify(r => r.UpdateAsync(productB), Times.Once);
        _productRepositoryMock.Verify(
            r => r.GetManyByIdForUpdateAsync(It.Is<Guid[]>(ids => ids.Length == 2)), Times.Once);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(order), Times.Once);
        _orderRepositoryMock.Verify(r => r.GetByIdForUpdateAsync(order.Id), Times.Once);
        _orderRepositoryMock.Verify(r => r.GetByIdAsync(order.Id), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
