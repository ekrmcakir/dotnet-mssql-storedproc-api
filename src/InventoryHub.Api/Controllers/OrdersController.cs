using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrdersController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    /// <summary>
    /// Creates a new order with line items in an atomic database transaction.
    /// Deducts inventory quantities and generates an order number.
    /// Uses dbo.sp_CreateOrderWithItems with Table-Valued Parameter.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderCreatedResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await _orderRepository.CreateOrderWithItemsAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById), 
            new { id = result.OrderId }, 
            ApiResponse<OrderCreatedResponse>.Ok(result, "Order created successfully.")
        );
    }

    /// <summary>
    /// Retrieves full order details including line items and customer information.
    /// Uses dbo.sp_GetOrderById (Multiple Result Sets).
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);
        if (order is null)
        {
            return NotFound(ApiResponse<object>.Fail($"Order with ID {id} was not found."));
        }

        return Ok(ApiResponse<OrderDto>.Ok(order));
    }
}

