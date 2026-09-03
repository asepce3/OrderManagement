using OrderManagement.Common;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Repositories;
using OrderManagement.Mappings;
using OrderManagement.DTOs.Queries;

namespace OrderManagement.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;

    public OrderService(IOrderRepository orderRepository, IProductRepository productRepository, IUnitOfWork unitOfWork, ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private static readonly Dictionary<string, string[]> AllowedTransitions = new()
    {
        { OrderStatus.Pending,   new[] { OrderStatus.Confirmed, OrderStatus.Cancelled } },
        { OrderStatus.Confirmed, new[] { OrderStatus.Shipped, OrderStatus.Cancelled } },
        { OrderStatus.Shipped,   new[] { OrderStatus.Delivered } },
        { OrderStatus.Delivered, Array.Empty<string>() }, // Terminal state
        { OrderStatus.Cancelled, Array.Empty<string>() }  // Terminal state
    };

    public async Task<Result<IEnumerable<OrderResponseDto>>> GetAllAsync(int userId, string role, GetOrdersRequestDto request)
    {
        var filter = new GetOrdersQueryFilter
        {
            UserId = role == UserRoles.Admin ? request.UserId : userId,
            Cursor = request.Cursor,
            PageSize = request.PageSize,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Direction = request.Direction,
            SortDirection = request.SortDirection,
        };

        var orders = await _orderRepository.GetAllAsync(filter);
        return Result<IEnumerable<OrderResponseDto>>.Success(orders.Select(o => o.ToResponseDto()));
    }

    public async Task<Result<OrderResponseDto>> GetByIdAsync(int userId, string role, Guid id)
    {
        var order = await _orderRepository.GetByIdAsync(id);
        if (order == null)
            return Result<OrderResponseDto>.Failure("Order not found.", StatusCodes.Status404NotFound);

        if (role != UserRoles.Admin && order.UserId != userId)
        {
            return Result<OrderResponseDto>.Failure("Order not found.", StatusCodes.Status404NotFound);
        }

        return Result<OrderResponseDto>.Success(order.ToResponseDto());
    }

    public async Task<Result<OrderResponseDto>> CreateAsync(CreateOrderDto dto, int userId)
    {
        _logger.LogInformation("Creating order for user {UserId} with {ItemCount} items", userId, dto.Items?.Count ?? 0);

        if (dto.Items == null || dto.Items.Count == 0)
        {
            _logger.LogWarning("Create order failed for user {UserId}: order items are required", userId);
            return Result<OrderResponseDto>.Failure("Order items are required.", StatusCodes.Status400BadRequest);
        }

        if (dto.Items.Any(i => i.Qty <= 0))
        {
            _logger.LogWarning("Create order failed for user {UserId}: quantity must be greater than zero", userId);
            return Result<OrderResponseDto>.Failure("Quantity must be greater than zero.", StatusCodes.Status400BadRequest);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var productIds = dto.Items.Select(o => o.ProductId).ToArray();
            var products = await _productRepository.GetManyByIdForUpdateAsync(productIds);
            var mapOfProducts = products.ToDictionary(o => o.Id, p => p);

            var details = new List<OrderDetail>();
            decimal totalPrice = 0;

            foreach (var item in dto.Items)
            {
                var exists = mapOfProducts.TryGetValue(item.ProductId, out var product);
                if (!exists || product == null)
                {
                    _logger.LogWarning("Create order failed for user {UserId}: product {ProductId} not found", userId, item.ProductId);
                    await _unitOfWork.RollbackAsync();
                    return Result<OrderResponseDto>.Failure($"Product with id '{item.ProductId}' not found.", StatusCodes.Status404NotFound);
                }

                if (product.StockQty < item.Qty)
                {
                    _logger.LogWarning("Create order failed for user {UserId}: insufficient stock for product {ProductId}", userId, item.ProductId);
                    await _unitOfWork.RollbackAsync();
                    return Result<OrderResponseDto>.Failure($"Insufficient stock for product '{product.Name}'.", StatusCodes.Status400BadRequest);
                }

                product.StockQty -= item.Qty;
                await _productRepository.UpdateAsync(product);

                var detail = item.ToEntity(product.Price);
                details.Add(detail);
                totalPrice += detail.SubTotal;
            }

            var order = dto.ToOrderEntity(userId, totalPrice, details);
            var createdOrder = await _orderRepository.CreateAsync(order);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order {OrderId} created successfully for user {UserId} with total price {TotalPrice}", createdOrder.Id, userId, createdOrder.TotalPrice);
            return Result<OrderResponseDto>.Success(createdOrder.ToResponseDto(), code: StatusCodes.Status201Created);
        }
        catch (Exception e)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(e, "Failed to create order for user {UserId}", userId);
            return Result<OrderResponseDto>.Failure("Failed to create order.", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<Result<OrderResponseDto>> UpdateStatusAsync(Guid id, UpdateOrderDto dto, int updatedBy, string role)
    {
        _logger.LogInformation("Updating status of order {OrderId} to {NewStatus} by user {UpdatedBy}", id, dto.Status, updatedBy);

        if (role != UserRoles.Admin)
        {
            _logger.LogWarning("User {UpdatedBy} attempted to update order {OrderId} status without admin role", updatedBy, id);
            return Result<OrderResponseDto>.Failure("You do not have permission to access this resource.", StatusCodes.Status403Forbidden);
        }

        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var order = await _orderRepository.GetByIdForUpdateAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Update status failed: order {OrderId} not found", id);
                await _unitOfWork.RollbackAsync();
                return Result<OrderResponseDto>.Failure("Order not found.", StatusCodes.Status404NotFound);
            }

            if(!IsStatusUpdateAllowed(order.Status, dto.Status))
            {
                _logger.LogWarning("Update status failed for order {OrderId}: invalid transition from {CurrentStatus} to {NewStatus}", id, order.Status, dto.Status);
                await _unitOfWork.RollbackAsync();
                return Result<OrderResponseDto>.Failure($"Invalid status transition from '{order.Status}' to '{dto.Status}'.", StatusCodes.Status400BadRequest);
            }

            if (dto.Status == OrderStatus.Cancelled)
            {
                var orderDetails = await _orderRepository.GetByIdAsync(id);
                if (orderDetails == null)
                {
                    await _unitOfWork.RollbackAsync();
                    return Result<OrderResponseDto>.Failure("Order not found.", StatusCodes.Status404NotFound);
                }

                var productIds = orderDetails.OrderDetails.Select(d => d.ProductId).ToArray();
                var products = await _productRepository.GetManyByIdForUpdateAsync(productIds);
                var mapOfProducts = products.ToDictionary(p => p.Id, p => p);

                foreach (var detail in orderDetails.OrderDetails)
                {
                    if (mapOfProducts.TryGetValue(detail.ProductId, out var product))
                    {
                        product.StockQty += detail.Qty;
                        await _productRepository.UpdateAsync(product);
                    }
                }
            }

            dto.UpdateEntity(order, updatedBy);
            await _orderRepository.UpdateAsync(order);

            await _unitOfWork.CommitAsync();

            _logger.LogInformation("Order {OrderId} status updated to {NewStatus} by user {UpdatedBy}", id, dto.Status, updatedBy);
            return Result<OrderResponseDto>.Success(order.ToResponseDto());
        }
        catch (Exception e)
        {
            await _unitOfWork.RollbackAsync();
            _logger.LogError(e, "Failed to update status of order {OrderId} by user {UpdatedBy}", id, updatedBy);
            return Result<OrderResponseDto>.Failure("Failed to update order.", StatusCodes.Status500InternalServerError);
        }
    }

    private static bool IsStatusUpdateAllowed(string current, string next)
    {
        var exists = AllowedTransitions.TryGetValue(current, out var statuses);

        return exists && statuses.Contains(next);
    }
}
