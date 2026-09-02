using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Common;
using OrderManagement.DTOs;
using OrderManagement.Models;
using OrderManagement.Services;
using System.Security.Claims;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<OrderResponseDto>>>> GetAll([FromQuery] GetOrdersRequestDto request)
    {
        var role = GetUserRole();
        var userId = GetUserId();
        if (role == null || userId == null)
            return Unauthorized(ApiResponse<OrderResponseDto>.ErrorResponse("User not authenticated."));
        var result = await _orderService.GetAllAsync((int)userId, role, request);
        return Ok(ApiResponse<IEnumerable<OrderResponseDto>>.SuccessResponse(result.Data!));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> GetById(Guid id)
    {
        var role = GetUserRole();
        var userId = GetUserId();
        if (role == null || userId == null)
            return Unauthorized(ApiResponse<OrderResponseDto>.ErrorResponse("User not authenticated."));
        var result = await _orderService.GetByIdAsync((int)userId, role, id);
        if (result.IsFailure)
            return StatusCode(result.Code, ApiResponse<OrderResponseDto>.ErrorResponse(result.Error!));

        return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(result.Data!));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> Create(CreateOrderDto dto)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<OrderResponseDto>.ErrorResponse("User not authenticated."));

        var result = await _orderService.CreateAsync(dto, userId.Value);
        if (result.IsFailure)
            return StatusCode(result.Code, ApiResponse<OrderResponseDto>.ErrorResponse(result.Error!));

        return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(result.Data!));
    }

    [HttpPut("status/{id:guid}")]
    [Authorize(Roles = UserRoles.Admin)]
    public async Task<ActionResult<ApiResponse<OrderResponseDto>>> UpdateStatus(Guid id, UpdateOrderDto dto)
    {
        var role = GetUserRole();
        var userId = GetUserId();
        if (role == null || userId == null)
            return Unauthorized(ApiResponse<OrderResponseDto>.ErrorResponse("User not authenticated."));

        var result = await _orderService.UpdateStatusAsync(id, dto, userId.Value, role);
        if (result.IsFailure)
            return StatusCode(result.Code, ApiResponse<OrderResponseDto>.ErrorResponse(result.Error!));

        return Ok(ApiResponse<OrderResponseDto>.SuccessResponse(result.Data!));
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }

    private string? GetUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
