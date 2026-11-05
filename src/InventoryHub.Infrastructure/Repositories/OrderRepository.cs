using System.Data;
using Dapper;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Exceptions;
using InventoryHub.Core.Interfaces;
using InventoryHub.Infrastructure.Data;
using Microsoft.Data.SqlClient;

namespace InventoryHub.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OrderRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<OrderCreatedResponse> CreateOrderWithItemsAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Build TVP DataTable
        var itemsTable = new DataTable();
        itemsTable.Columns.Add("ProductId", typeof(int));
        itemsTable.Columns.Add("Quantity", typeof(int));
        itemsTable.Columns.Add("UnitPrice", typeof(decimal));

        foreach (var item in request.Items)
        {
            itemsTable.Rows.Add(item.ProductId, item.Quantity, item.UnitPrice);
        }

        // 2. Prepare Dynamic Parameters with Table-Valued Parameter and Output parameters
        var parameters = new DynamicParameters();
        parameters.Add("@CustomerId", request.CustomerId, DbType.Int32);
        parameters.Add("@Items", itemsTable.AsTableValuedParameter("dbo.OrderItemTableType"));
        parameters.Add("@NewOrderId", dbType: DbType.Int32, direction: ParameterDirection.Output);
        parameters.Add("@OrderNumber", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var command = new CommandDefinition(
                StoredProcedureNames.CreateOrderWithItems,
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );

            await connection.ExecuteAsync(command);

            return new OrderCreatedResponse
            {
                OrderId = parameters.Get<int>("@NewOrderId"),
                OrderNumber = parameters.Get<string>("@OrderNumber")
            };
        }
        catch (SqlException ex) when (ex.Number == 50010)
        {
            throw new NotFoundException("Customer", request.CustomerId);
        }
        catch (SqlException ex) when (ex.Number is 50011 or 50012 or >= 50000)
        {
            throw new BusinessRuleException(ex.Message, ex.Number);
        }
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            StoredProcedureNames.GetOrderById,
            new { OrderId = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        using var multi = await connection.QueryMultipleAsync(command);
        var order = await multi.ReadSingleOrDefaultAsync<OrderDto>();

        if (order is not null)
        {
            var items = (await multi.ReadAsync<OrderItemDto>()).ToList();
            order.Items = items;
        }

        return order;
    }
}

