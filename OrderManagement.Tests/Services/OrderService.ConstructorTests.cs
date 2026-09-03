namespace OrderManagement.Tests.Services;

public partial class OrderServiceTests
{
    [Fact]
    public void Constructor_NullOrderRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrderService(
                null!,
                _productRepositoryMock.Object,
                _unitOfWorkMock.Object,
                _logger));

        Assert.Equal("orderRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullProductRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrderService(
                _orderRepositoryMock.Object,
                null!,
                _unitOfWorkMock.Object,
                _logger));

        Assert.Equal("productRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrderService(
                _orderRepositoryMock.Object,
                _productRepositoryMock.Object,
                null!,
                _logger));

        Assert.Equal("unitOfWork", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new OrderService(
                _orderRepositoryMock.Object,
                _productRepositoryMock.Object,
                _unitOfWorkMock.Object,
                null!));

        Assert.Equal("logger", exception.ParamName);
    }
}
