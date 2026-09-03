namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenItemsIsNull_ReturnsBadRequestAndDoesNotStartTransaction()
    {
        var dto = new CreateOrderDto { Items = null! };

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        Assert.Equal("Order items are required.", result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        _orderRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenItemsIsEmpty_ReturnsBadRequestAndDoesNotStartTransaction()
    {
        var dto = BuildCreateOrderDto();

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        Assert.Equal("Order items are required.", result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CreateAsync_WhenAnyItemHasNonPositiveQuantity_ReturnsBadRequest(int qty)
    {
        var dto = BuildCreateOrderDto((Guid.NewGuid(), qty));

        var result = await CreateService().CreateAsync(dto, userId: 1);

        Assert.True(result.IsFailure);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
        Assert.Equal("Quantity must be greater than zero.", result.Error);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
