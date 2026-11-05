using System.Data;
using Dapper;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Exceptions;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using InventoryHub.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace InventoryHub.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ProductRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(ProductQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@SearchTerm", queryParams.SearchTerm, DbType.String);
        parameters.Add("@CategoryId", queryParams.CategoryId, DbType.Int32);
        parameters.Add("@PageNumber", queryParams.PageNumber < 1 ? 1 : queryParams.PageNumber, DbType.Int32);
        parameters.Add("@PageSize", queryParams.PageSize < 1 ? 10 : queryParams.PageSize, DbType.Int32);
        parameters.Add("@TotalRecords", dbType: DbType.Int32, direction: ParameterDirection.Output);

        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            StoredProcedureNames.GetProductsPaged,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        var items = (await connection.QueryAsync<ProductDto>(command)).ToList();
        int totalRecords = parameters.Get<int>("@TotalRecords");

        return new PagedResult<ProductDto>(items, totalRecords, queryParams.PageNumber, queryParams.PageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            StoredProcedureNames.GetProductById,
            new { ProductId = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        return await connection.QuerySingleOrDefaultAsync<ProductDto>(command);
    }

    public async Task<int> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@CategoryId", request.CategoryId, DbType.Int32);
        parameters.Add("@Sku", request.Sku, DbType.String);
        parameters.Add("@Name", request.Name, DbType.String);
        parameters.Add("@Description", request.Description, DbType.String);
        parameters.Add("@Price", request.Price, DbType.Decimal);
        parameters.Add("@StockQuantity", request.StockQuantity, DbType.Int32);
        parameters.Add("@NewProductId", dbType: DbType.Int32, direction: ParameterDirection.Output);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(
                StoredProcedureNames.CreateProduct,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(command);
            return parameters.Get<int>("@NewProductId");
        }
        catch (SqlException ex) when (ex.Number is 50001 or 50002 or >= 50000)
        {
            throw new BusinessRuleException(ex.Message, ex.Number);
        }
    }

    public async Task UpdateAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@ProductId", id, DbType.Int32);
        parameters.Add("@CategoryId", request.CategoryId, DbType.Int32);
        parameters.Add("@Name", request.Name, DbType.String);
        parameters.Add("@Description", request.Description, DbType.String);
        parameters.Add("@Price", request.Price, DbType.Decimal);
        parameters.Add("@StockQuantity", request.StockQuantity, DbType.Int32);
        parameters.Add("@IsActive", request.IsActive, DbType.Boolean);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(
                StoredProcedureNames.UpdateProduct,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex) when (ex.Number == 50003)
        {
            throw new NotFoundException("Product", id);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            throw new BusinessRuleException(ex.Message, ex.Number);
        }
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(
                StoredProcedureNames.DeleteProduct,
                new { ProductId = id },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex) when (ex.Number == 50003)
        {
            throw new NotFoundException("Product", id);
        }
    }
}

