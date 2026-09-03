namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Pending, OrderStatus.Pending)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending, "Completed")]
    public async Task UpdateStatusAsync_WhenTransitionIsInvalid_RollsBackAndReturnsBadRequest(
        string currentStatus,
        string nextStatus)
    {
        var order = BuildOrder(status: currentStatus);
        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(nextStatus),
            updatedBy: 1,
            role: UserRoles.Admin);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        Assert.Contains(currentStatus, result.Error);
        Assert.Contains(nextStatus, result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
        _productRepositoryMock.Verify(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()), Times.Never);
    }

    [Theory]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Pending)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Cancelled)]
    public async Task UpdateStatusAsync_FromTerminalState_RejectsAnyTransition(
        string currentStatus,
        string nextStatus)
    {
        var order = BuildOrder(status: currentStatus);
        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(order.Id))
            .ReturnsAsync(order);

        var result = await CreateService().UpdateStatusAsync(
            order.Id,
            BuildUpdateOrderDto(nextStatus),
            updatedBy: 1,
            role: UserRoles.Admin);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
