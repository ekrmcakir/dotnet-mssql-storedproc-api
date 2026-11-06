using FluentAssertions;
using InventoryHub.Api.Controllers;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryHub.Tests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepo;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _mockOrderRepo = new Mock<IOrderRepository>();
        _controller = new OrdersController(_mockOrderRepo.Object);
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WhenOrderIsCreated()
    {
        // Arrange
        var request = new CreateOrderRequest
        {
            CustomerId = 1,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2, UnitPrice = 99.99m }
            }
        };

        var responseData = new OrderCreatedResponse
        {
            OrderId = 101,
            OrderNumber = "ORD-20261105-ABC12345"
        };

        _mockOrderRepo
            .Setup(r => r.CreateOrderWithItemsAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(responseData);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(OrdersController.GetById));
        createdResult.RouteValues!["id"].Should().Be(101);

        var apiResponse = createdResult.Value.Should().BeOfType<ApiResponse<OrderCreatedResponse>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.OrderNumber.Should().Be("ORD-20261105-ABC12345");
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenOrderDoesNotExist()
    {
        // Arrange
        _mockOrderRepo
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDto?)null);

        // Act
        var result = await _controller.GetById(999, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
    }
}

