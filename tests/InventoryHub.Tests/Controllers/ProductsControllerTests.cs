using FluentAssertions;
using InventoryHub.Api.Controllers;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace InventoryHub.Tests.Controllers;

public class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _mockProductRepo;
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _mockProductRepo = new Mock<IProductRepository>();
        _controller = new ProductsController(_mockProductRepo.Object);
    }

    [Fact]
    public async Task GetPaged_ReturnsOkResult_WithPagedData()
    {
        // Arrange
        var queryParams = new ProductQueryParams { PageNumber = 1, PageSize = 10 };
        var sampleProducts = new List<ProductDto>
        {
            new() { ProductId = 1, Name = "Laptop", Price = 1500m, StockQuantity = 10, Sku = "SKU-1", CategoryName = "Electronics" },
            new() { ProductId = 2, Name = "Mouse", Price = 50m, StockQuantity = 20, Sku = "SKU-2", CategoryName = "Accessories" }
        };
        var pagedResult = new PagedResult<ProductDto>(sampleProducts, totalRecords: 2, pageNumber: 1, pageSize: 10);

        _mockProductRepo
            .Setup(repo => repo.GetPagedAsync(queryParams, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetPaged(queryParams, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var apiResponse = okResult.Value.Should().BeOfType<ApiResponse<PagedResult<ProductDto>>>().Subject;
        apiResponse.Success.Should().BeTrue();
        apiResponse.Data!.Items.Should().HaveCount(2);
        apiResponse.Data.TotalRecords.Should().Be(2);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        int nonExistentId = 999;
        _mockProductRepo
            .Setup(repo => repo.GetByIdAsync(nonExistentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductDto?)null);

        // Act
        var result = await _controller.GetById(nonExistentId, CancellationToken.None);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        var response = notFoundResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;
        response.Success.Should().BeFalse();
        response.Message.Should().Contain("999 was not found");
    }

    [Fact]
    public async Task GetById_ReturnsOk_WhenProductExists()
    {
        // Arrange
        int productId = 1;
        var product = new ProductDto
        {
            ProductId = productId,
            Name = "Monitor",
            Price = 300m,
            StockQuantity = 5,
            Sku = "SKU-MON",
            CategoryName = "Electronics"
        };

        _mockProductRepo
            .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        var result = await _controller.GetById(productId, CancellationToken.None);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ProductDto>>().Subject;
        response.Success.Should().BeTrue();
        response.Data!.Name.Should().Be("Monitor");
    }

    [Fact]
    public async Task Create_ReturnsCreatedAtAction_WithNewId()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            CategoryId = 1,
            Sku = "SKU-NEW",
            Name = "Wireless Headset",
            Price = 120m,
            StockQuantity = 15
        };

        _mockProductRepo
            .Setup(repo => repo.CreateAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var createdAtResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdAtResult.ActionName.Should().Be(nameof(ProductsController.GetById));
        createdAtResult.RouteValues!["id"].Should().Be(42);
    }
}

