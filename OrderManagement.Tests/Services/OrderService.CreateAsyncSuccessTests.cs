namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenValid_DeductsStockCreatesOrderAndReturnsCreated()
    {
        const int userId = 7;
        var product = BuildProduct(Guid.NewGuid(), name: "Mouse", stockQty: 10, price: 100m);
        var dto = BuildCreateOrderDto((product.Id, 3));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { product });
        _orderRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        var result = await CreateService().CreateAsync(dto, userId);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status201Created, result.Code);
        Assert.Equal(7, product.StockQty); // 10 - 3

        var response = result.Data;
        Assert.NotNull(response);
        Assert.Equal(OrderStatus.Pending, response!.Status);
        Assert.Equal(300m, response.TotalPrice);
        Assert.Equal(userId, response.UserId);

        var item = Assert.Single(response.Items);
        Assert.Equal(product.Id, item.ProductId);
        Assert.Equal(3, item.Qty);
        Assert.Equal(100m, item.Price);
        Assert.Equal(300m, item.SubTotal);

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.Is<Order>(o =>
            o.Status == OrderStatus.Pending &&
            o.UserId == userId &&
            o.TotalPrice == 300m &&
            o.CreatedBy == userId &&
            o.UpdatedBy == userId &&
            o.OrderDetails.Count == 1)), Times.Once);

        _productRepositoryMock.Verify(r => r.UpdateAsync(product), Times.Once);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithMultipleItems_ComputesTotalPriceAndDeductsAllStocks()
    {
        var productA = BuildProduct(Guid.NewGuid(), name: "Monitor", stockQty: 10, price: 500m);
        var productB = BuildProduct(Guid.NewGuid(), name: "Webcam", stockQty: 20, price: 200m);
        var dto = BuildCreateOrderDto((productA.Id, 2), (productB.Id, 4));

        _productRepositoryMock
            .Setup(r => r.GetManyByIdForUpdateAsync(It.IsAny<Guid[]>()))
            .ReturnsAsync(new List<Product> { productA, productB });
        _orderRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>()))
            .ReturnsAsync((Order order) => order);

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(8, productA.StockQty);  // 10 - 2
        Assert.Equal(16, productB.StockQty); // 20 - 4

        var response = result.Data;
        Assert.NotNull(response);
        Assert.Equal(1800m, response!.TotalPrice); // (2 * 500) + (4 * 200)
        Assert.Equal(2, response.Items.Count);

        _orderRepositoryMock.Verify(r => r.CreateAsync(It.Is<Order>(o =>
            o.TotalPrice == 1800m &&
            o.OrderDetails.Count == 2 &&
            o.OrderDetails.All(d => d.SubTotal == d.Qty * d.Price))), Times.Once);
        _productRepositoryMock.Verify(r => r.UpdateAsync(productA), Times.Once);
        _productRepositoryMock.Verify(r => r.UpdateAsync(productB), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
