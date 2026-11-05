using System.Data;
using Dapper;
using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Infrastructure.Data;

namespace InventoryHub.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DashboardRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        using var connection = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            StoredProcedureNames.GetDashboardSummary,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );

        using var multi = await connection.QueryMultipleAsync(command);

        var kpis = await multi.ReadSingleOrDefaultAsync<DashboardKpisDto>() ?? new DashboardKpisDto();
        var topSelling = (await multi.ReadAsync<TopSellingProductDto>()).ToList();
        var lowStock = (await multi.ReadAsync<LowStockProductDto>()).ToList();

        return new DashboardSummaryDto
        {
            Kpis = kpis,
            TopSellingProducts = topSelling,
            LowStockAlerts = lowStock
        };
    }
}

