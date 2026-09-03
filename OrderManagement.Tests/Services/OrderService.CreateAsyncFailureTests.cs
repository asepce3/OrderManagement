namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenProductDoesNotExist_RollsBackAndReturnsNotFound()
    {
        var missingProductId = Guid.NewGuid();
        var dto = BuildCreateOrderDto((missingProductId, 2));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product>());

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Contains(missingProductId.ToString(), result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _productRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Product>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenStockIsInsufficient_RollsBackAndReturnsBadRequest()
    {
        var product = BuildProduct(Guid.NewGuid(), name: "Keyboard", stockQty: 1, price: 150m);
        var dto = BuildCreateOrderDto((product.Id, 2));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { product });

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        Assert.Contains("Keyboard", result.Error);
        Assert.Equal(1, product.StockQty); // stock must stay untouched
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Never);
        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenLaterItemIsMissing_RollsBackAndReturnsNotFound()
    {
        var existingProduct = BuildProduct(Guid.NewGuid(), name: "Speaker", stockQty: 10, price: 100m);
        var missingProductId = Guid.NewGuid();
        var dto = BuildCreateOrderDto((existingProduct.Id, 1), (missingProductId, 1));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { existingProduct });
        _orderRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status404NotFound, result.Code);
        Assert.Contains(missingProductId.ToString(), result.Error);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
    }
}
