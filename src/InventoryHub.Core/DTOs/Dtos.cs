using System.ComponentModel.DataAnnotations;

namespace InventoryHub.Core.DTOs;

// ---------------- Product DTOs ----------------
public class ProductDto
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductQueryParams
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class CreateProductRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required, StringLength(50, MinimumLength = 3)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    public int StockQuantity { get; set; }
}

public class UpdateProductRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0.01, 999999.99)]
    public decimal Price { get; set; }

    [Range(0, 1000000)]
    public int StockQuantity { get; set; }

    public bool IsActive { get; set; } = true;
}

// ---------------- Order DTOs ----------------
public class CreateOrderItemRequest
{
    [Required]
    public int ProductId { get; set; }

    [Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000.")]
    public int Quantity { get; set; }

    [Range(0.01, 999999.99, ErrorMessage = "UnitPrice must be greater than zero.")]
    public decimal UnitPrice { get; set; }
}

public class CreateOrderRequest
{
    [Required]
    public int CustomerId { get; set; }

    [Required, MinLength(1, ErrorMessage = "Order must contain at least one item.")]
    public List<CreateOrderItemRequest> Items { get; set; } = new();
}

public class OrderCreatedResponse
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
}

public class OrderItemDto
{
    public int OrderItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

public class OrderDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}

// ---------------- Dashboard / Analytics DTOs ----------------
public class DashboardSummaryDto
{
    public DashboardKpisDto Kpis { get; set; } = new();
    public List<TopSellingProductDto> TopSellingProducts { get; set; } = new();
    public List<LowStockProductDto> LowStockAlerts { get; set; } = new();
}

public class DashboardKpisDto
{
    public int TotalActiveProducts { get; set; }
    public int TotalOrdersCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public int LowStockProductsCount { get; set; }
}

public class TopSellingProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal TotalSalesRevenue { get; set; }
}

public class LowStockProductDto
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
}

