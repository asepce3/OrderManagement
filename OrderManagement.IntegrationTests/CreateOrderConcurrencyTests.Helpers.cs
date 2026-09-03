namespace OrderManagement.IntegrationTests;

public partial class CreateOrderConcurrencyTests
{
    private async Task<ConcurrencyOutcome> RunConcurrentOrdersAsync(int participantCount)
    {
        // Seed: one product with limited stock + one distinct user per concurrent request.
        await using var seedContext = _database.CreateDbContext();
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = $"Concurrency Product {Guid.NewGuid():N}"[..40],
            Price = ProductPrice,
            StockQty = StockQty,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        seedContext.Products.Add(product);

        var users = Enumerable.Range(1, participantCount)
            .Select(i => new User
            {
                Email = $"race-user-{i}-{Guid.NewGuid():N}@example.com",
                Name = $"Race User {i}",
                Address = "Test Address",
                Password = "not-used",
                Role = UserRoles.User,
            })
            .ToList();
        seedContext.Users.AddRange(users);
        await seedContext.SaveChangesAsync();

        var productId = product.Id;
        var userIds = users.Select(u => u.Id).ToArray();

        // Fire all requests at (almost) the same moment so they contend on the product row lock.
        using var startBarrier = new Barrier(participantCount);
        var tasks = Enumerable.Range(0, participantCount)
            .Select(i => Task.Run(async () =>
            {
                var (service, context) = CreateServiceScope();
                try
                {
                    if (!startBarrier.SignalAndWait(TimeSpan.FromSeconds(30)))
                    {
                        throw new InvalidOperationException("Barrier timed out while waiting for participants to start.");
                    }

                    var dto = new CreateOrderDto
                    {
                        Items = new List<CreateOrderItemDto>
                        {
                            new() { ProductId = productId, Qty = QtyPerOrder },
                        },
                    };

                    return await service.CreateAsync(dto, userIds[i]);
                }
                finally
                {
                    await context.DisposeAsync();
                }
            }))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Verify persisted state using a fresh context (no cached entity state).
        await using var verifyContext = _database.CreateDbContext();
        var storedProduct = await verifyContext.Products.AsNoTracking().SingleAsync(p => p.Id == productId);
        var persistedOrders = await verifyContext.Orders
            .AsNoTracking()
            .Where(o => o.OrderDetails.Any(d => d.ProductId == productId))
            .CountAsync();

        return new ConcurrencyOutcome(results, storedProduct.StockQty, persistedOrders);
    }

    private (OrderService Service, AppDbContext Context) CreateServiceScope()
    {
        var context = _database.CreateDbContext();
        var service = new OrderService(
            new OrderRepository(context),
            new ProductRepository(context),
            new UnitOfWork(context),
            NullLogger<OrderService>.Instance);

        return (service, context);
    }

    private static void AssertAllFailuresAreInsufficientStock(IEnumerable<Result<OrderResponseDto>> results)
    {
        foreach (var result in results.Where(r => r.IsFailure))
        {
            Assert.Equal(StatusCodes.Status400BadRequest, result.Code);
            Assert.NotNull(result.Error);
            Assert.Contains("Insufficient stock", result.Error);
        }
    }

    private static void AssertAllSuccessesAreCreated(IEnumerable<Result<OrderResponseDto>> results)
    {
        foreach (var result in results.Where(r => r.IsSuccess))
        {
            Assert.Equal(StatusCodes.Status201Created, result.Code);
            Assert.NotNull(result.Data);
            Assert.Equal(QtyPerOrder * ProductPrice, result.Data!.TotalPrice);
            var item = Assert.Single(result.Data.Items);
            Assert.Equal(QtyPerOrder, item.Qty);
        }
    }
}
