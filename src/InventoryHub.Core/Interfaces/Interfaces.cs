using System.Data;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Entities;
using InventoryHub.Core.Models;

namespace InventoryHub.Core.Interfaces;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public interface IProductRepository
{
    Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface IOrderRepository
{
    Task<OrderCreatedResponse> CreateOrderWithItemsAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDashboardRepository
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

