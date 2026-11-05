namespace InventoryHub.Infrastructure.Data;

public static class StoredProcedureNames
{
    public const string GetProductsPaged = "dbo.sp_GetProductsPaged";
    public const string GetProductById = "dbo.sp_GetProductById";
    public const string CreateProduct = "dbo.sp_CreateProduct";
    public const string UpdateProduct = "dbo.sp_UpdateProduct";
    public const string DeleteProduct = "dbo.sp_DeleteProduct";
    public const string CreateOrderWithItems = "dbo.sp_CreateOrderWithItems";
    public const string GetOrderById = "dbo.sp_GetOrderById";
    public const string GetDashboardSummary = "dbo.sp_GetDashboardSummary";
}

