using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// Retrieves a paginated list of active products with optional search and category filters.
    /// Uses dbo.sp_GetProductsPaged.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged([FromQuery] ProductQueryParams queryParams, CancellationToken cancellationToken)
    {
        var result = await _productRepository.GetPagedAsync(queryParams, cancellationToken);
        return Ok(ApiResponse<PagedResult<ProductDto>>.Ok(result));
    }

    /// <summary>
    /// Retrieves details of a specific product by ID.
    /// Uses dbo.sp_GetProductById.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return NotFound(ApiResponse<object>.Fail($"Product with ID {id} was not found."));
        }

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>
    /// Creates a new product.
    /// Uses dbo.sp_CreateProduct.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var productId = await _productRepository.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById), 
            new { id = productId }, 
            ApiResponse<object>.Ok(new { ProductId = productId }, "Product created successfully.")
        );
    }

    /// <summary>
    /// Updates an existing product.
    /// Uses dbo.sp_UpdateProduct.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await _productRepository.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { ProductId = id }, "Product updated successfully."));
    }

    /// <summary>
    /// Soft deletes a product by marking it inactive.
    /// Uses dbo.sp_DeleteProduct.
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _productRepository.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { ProductId = id }, "Product deleted successfully."));
    }
}

