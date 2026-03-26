using System.Security.Claims;
using ECommerce.Api.DTOs;
using ECommerce.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrderController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrderController> _logger;

    public OrderController(OrderService orderService, ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var order = await _orderService.CreateOrder(userId, request.Items, request.DiscountCode);
        if (order == null)
        {
            return BadRequest(new { message = "Could not create order. Check product availability." });
        }

        return CreatedAtAction(nameof(GetOrder), new { orderId = order.Id }, order);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetOrder(int orderId)
    {
        var order = await _orderService.GetOrderById(orderId);
        if (order == null)
            return NotFound(new { message = "Order not found" });

        return Ok(order);
    }

    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var orders = await _orderService.GetUserOrders(userId);
        return Ok(orders);
    }

    [HttpPut("{orderId}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int orderId, [FromBody] string newStatus)
    {
        var result = await _orderService.UpdateOrderStatus(orderId, newStatus);
        if (!result)
            return NotFound(new { message = "Order not found" });

        return Ok(new { message = "Order status updated" });
    }

    [HttpPost("{orderId}/cancel")]
    public async Task<IActionResult> CancelOrder(int orderId)
    {
        var result = await _orderService.CancelOrder(orderId);
        if (!result)
            return BadRequest(new { message = "Order cannot be cancelled" });

        return Ok(new { message = "Order cancelled successfully" });
    }
}
