namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_WhenCancellingAndDetailsCannotBeLoaded_RollsBackAndReturnsNotFound()
    {
        var order = BuildOrder(status: OrderStatus.Pending);
        _orderRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(order.Id)).ReturnsAsync(order);
        _orderRepositoryMock.Setup(r => r.GetByIdAsync(order.Id)).ReturnsAsync((Order?)null);

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(OrderStatus.Cancelled),
            updatedBy: 1,
            role: UserRoles.Admin);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Equal("Order not found.", result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        _productRepositoryMock.Verify(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenUnexpectedExceptionOccurs_RollsBackAndReturnsInternalServerError()
    {
        var order = BuildOrder(status: OrderStatus.Pending);
        _orderRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(order.Id)).ReturnsAsync(order);
        _orderRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Order>()))
            .ThrowsAsync(new InvalidOperationException("update failed"));

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(OrderStatus.Confirmed),
            updatedBy: 1,
            role: UserRoles.Admin);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.Code);
        Assert.Equal("Failed to update order.", result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Once);
    }
}
