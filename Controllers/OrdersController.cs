using Microsoft.AspNetCore.Mvc;
using OrderManagement.DTOs;
using OrderManagement.Services;

namespace OrderManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderResponseDto>> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
            return NotFound();

        return Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<OrderResponseDto>> Create(CreateOrderDto dto)
    {
        var created = await _orderService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateOrderDto dto)
    {
        var updated = await _orderService.UpdateAsync(id, dto);
        if (!updated)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _orderService.DeleteAsync(id);
        if (!deleted)
            return NotFound();

        return NoContent();
    }
}
