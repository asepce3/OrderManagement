using OrderManagement.IntegrationTests.Infrastructure;

namespace OrderManagement.IntegrationTests;

public partial class CreateOrderConcurrencyTests : IAsyncLifetime
{
    private const int StockQty = 7;
    private const int QtyPerOrder = 5;
    private const decimal ProductPrice = 100_000m;

    private readonly TestDatabase _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync().AsTask();

    [Fact]
    public async Task CreateOrder_WhenTwoUsersOrderSameProductConcurrently_ExactlyOneSucceeds()
    {
        var outcome = await RunConcurrentOrdersAsync(participantCount: 2);

        Assert.Equal(1, outcome.SuccessCount);
        Assert.Equal(1, outcome.FailureCount);
        AssertAllFailuresAreInsufficientStock(outcome.Results);
        AssertAllSuccessesAreCreated(outcome.Results);

        Assert.Equal(StockQty - QtyPerOrder, outcome.FinalStockQty); // 7 - 5 = 2
        Assert.Equal(1, outcome.PersistedOrderCount);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(8)]
    public async Task CreateOrder_WhenManyUsersRaceForSameStock_NeverOversells(int participantCount)
    {
        const int expectedSuccessCount = StockQty / QtyPerOrder; // 7 / 5 => at most one may succeed

        var outcome = await RunConcurrentOrdersAsync(participantCount);

        Assert.Equal(expectedSuccessCount, outcome.SuccessCount);
        Assert.Equal(participantCount - expectedSuccessCount, outcome.FailureCount);
        AssertAllFailuresAreInsufficientStock(outcome.Results);
        AssertAllSuccessesAreCreated(outcome.Results);

        Assert.Equal(expectedSuccessCount * QtyPerOrder, StockQty - outcome.FinalStockQty);
        Assert.True(outcome.FinalStockQty >= 0, "Product stock must never go negative.");
        Assert.Equal(expectedSuccessCount, outcome.PersistedOrderCount);
    }

    private sealed record ConcurrencyOutcome(
        Result<OrderResponseDto>[] Results,
        int FinalStockQty,
        int PersistedOrderCount)
    {
        public int SuccessCount => Results.Count(r => r.IsSuccess);
        public int FailureCount => Results.Count(r => r.IsFailure);
    }
}
