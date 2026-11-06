using InventoryHub.Core.DTOs;
using InventoryHub.Core.Interfaces;
using InventoryHub.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace InventoryHub.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _dashboardRepository;

    public DashboardController(IDashboardRepository dashboardRepository)
    {
        _dashboardRepository = dashboardRepository;
    }

    /// <summary>
    /// Retrieves executive dashboard analytics including KPIs, top selling products,
    /// and low stock inventory alerts in a single database round-trip.
    /// Uses dbo.sp_GetDashboardSummary (Multiple Result Sets).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardRepository.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }
}

