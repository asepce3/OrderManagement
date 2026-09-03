namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task UpdateStatusAsync_WhenCallerIsNotAdmin_ReturnsForbiddenAndDoesNotStartTransaction()
    {
        var orderId = Guid.NewGuid();
        var dto = BuildUpdateOrderDto(OrderStatus.Confirmed);

        var result = await CreateService().UpdateStatusAsync(orderId, dto, updatedBy: 1, role: UserRoles.User);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status403Forbidden, result.Code);
        Assert.Equal("You do not have permission to access this resource.", result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.GetByIdForUpdateAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenOrderDoesNotExist_RollsBackAndReturnsNotFound()
    {
        var orderId = Guid.NewGuid();
        _orderRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(orderId))
            .ReturnsAsync((Order?)null);

        var result = await CreateService().UpdateStatusAsync(
            orderId,
            BuildUpdateOrderDto(OrderStatus.Confirmed),
            updatedBy: 1,
            role: UserRoles.Admin);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Equal("Order not found.", result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>()), Times.Never);
    }
}
