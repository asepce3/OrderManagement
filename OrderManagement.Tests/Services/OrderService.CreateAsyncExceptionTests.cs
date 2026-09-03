namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenRepositoryThrows_RollsBackAndReturnsInternalServerError()
    {
        var dto = BuildCreateOrderDto((Guid.NewGuid(), 1));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ThrowsAsync(new InvalidOperationException("database unavailable"));

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.Code);
        Assert.Equal("Failed to create order.", result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenCommitFails_RollsBackAndReturnsInternalServerError()
    {
        var product = BuildProduct(Guid.NewGuid(), stockQty: 5, price: 50m);
        var dto = BuildCreateOrderDto((product.Id, 1));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { product });
        _orderRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);
        _unitOfWorkMock
            .Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("commit failed"));

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status500InternalServerError, result.Code);
        Assert.Equal("Failed to create order.", result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Once);
    }
}
