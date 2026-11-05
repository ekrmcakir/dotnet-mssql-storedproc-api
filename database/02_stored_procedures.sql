-- ============================================================================
-- InventoryHub: Stored Procedures
-- Includes: CRUD, Pagination, Transactions, TVP (Table-Valued Parameter),
--           Multiple Result Sets, Error Handling (TRY/CATCH)
-- ============================================================================

USE InventoryDb;
GO

-- 1. Paged Products with Search & Category Filter
CREATE OR ALTER PROCEDURE dbo.sp_GetProductsPaged
    @SearchTerm NVARCHAR(100) = NULL,
    @CategoryId INT = NULL,
    @PageNumber INT = 1,
    @PageSize INT = 10,
    @TotalRecords INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- Calculate total matching records for pagination metadata
    SELECT @TotalRecords = COUNT(1)
    FROM dbo.Products p
    WHERE p.IsActive = 1
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%' OR p.Sku LIKE '%' + @SearchTerm + '%');

    -- Fetch paginated slice with Category name
    SELECT 
        p.ProductId,
        p.CategoryId,
        c.Name AS CategoryName,
        p.Sku,
        p.Name,
        p.Description,
        p.Price,
        p.StockQuantity,
        p.IsActive,
        p.CreatedAt,
        p.UpdatedAt
    FROM dbo.Products p
    INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
    WHERE p.IsActive = 1
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND (@SearchTerm IS NULL OR p.Name LIKE '%' + @SearchTerm + '%' OR p.Sku LIKE '%' + @SearchTerm + '%')
    ORDER BY p.ProductId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;
END;
GO

-- 2. Get Product By Id
CREATE OR ALTER PROCEDURE dbo.sp_GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.ProductId,
        p.CategoryId,
        c.Name AS CategoryName,
        p.Sku,
        p.Name,
        p.Description,
        p.Price,
        p.StockQuantity,
        p.IsActive,
        p.CreatedAt,
        p.UpdatedAt
    FROM dbo.Products p
    INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
    WHERE p.ProductId = @ProductId;
END;
GO

-- 3. Create Product
CREATE OR ALTER PROCEDURE dbo.sp_CreateProduct
    @CategoryId INT,
    @Sku NVARCHAR(50),
    @Name NVARCHAR(150),
    @Description NVARCHAR(500) = NULL,
    @Price DECIMAL(18, 2),
    @StockQuantity INT,
    @NewProductId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    -- SKU uniqueness check
    IF EXISTS (SELECT 1 FROM dbo.Products WHERE Sku = @Sku)
    BEGIN
        THROW 50001, 'A product with this SKU already exists.', 1;
        RETURN;
    END

    -- Category existence check
    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @CategoryId)
    BEGIN
        THROW 50002, 'The specified category does not exist.', 1;
        RETURN;
    END

    INSERT INTO dbo.Products (CategoryId, Sku, Name, Description, Price, StockQuantity, IsActive, CreatedAt)
    VALUES (@CategoryId, @Sku, @Name, @Description, @Price, @StockQuantity, 1, SYSUTCDATETIME());

    SET @NewProductId = SCOPE_IDENTITY();
END;
GO

-- 4. Update Product
CREATE OR ALTER PROCEDURE dbo.sp_UpdateProduct
    @ProductId INT,
    @CategoryId INT,
    @Name NVARCHAR(150),
    @Description NVARCHAR(500) = NULL,
    @Price DECIMAL(18, 2),
    @StockQuantity INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @ProductId)
    BEGIN
        THROW 50003, 'Product not found.', 1;
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.Categories WHERE CategoryId = @CategoryId)
    BEGIN
        THROW 50002, 'The specified category does not exist.', 1;
        RETURN;
    END

    UPDATE dbo.Products
    SET CategoryId = @CategoryId,
        Name = @Name,
        Description = @Description,
        Price = @Price,
        StockQuantity = @StockQuantity,
        IsActive = @IsActive,
        UpdatedAt = SYSUTCDATETIME()
    WHERE ProductId = @ProductId;
END;
GO

-- 5. Delete (Soft Delete) Product
CREATE OR ALTER PROCEDURE dbo.sp_DeleteProduct
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Products WHERE ProductId = @ProductId)
    BEGIN
        THROW 50003, 'Product not found.', 1;
        RETURN;
    END

    UPDATE dbo.Products
    SET IsActive = 0,
        UpdatedAt = SYSUTCDATETIME()
    WHERE ProductId = @ProductId;
END;
GO

-- 6. Create Order With Items (Atomic Transaction with TVP and Stock Decrement)
CREATE OR ALTER PROCEDURE dbo.sp_CreateOrderWithItems
    @CustomerId INT,
    @Items dbo.OrderItemTableType READONLY,
    @NewOrderId INT OUTPUT,
    @OrderNumber NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Validate customer existence
        IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerId = @CustomerId)
        BEGIN
            THROW 50010, 'Customer does not exist.', 1;
        END

        -- 2. Validate items collection is not empty
        IF NOT EXISTS (SELECT 1 FROM @Items)
        BEGIN
            THROW 50011, 'Cannot create an order without items.', 1;
        END

        -- 3. Check stock availability for all requested items
        IF EXISTS (
            SELECT 1 
            FROM @Items i
            INNER JOIN dbo.Products p ON i.ProductId = p.ProductId
            WHERE p.StockQuantity < i.Quantity OR p.IsActive = 0
        )
        BEGIN
            THROW 50012, 'One or more items do not have sufficient stock or are inactive.', 1;
        END

        -- 4. Calculate total amount
        DECLARE @TotalAmount DECIMAL(18, 2);
        SELECT @TotalAmount = SUM(Quantity * UnitPrice) FROM @Items;

        -- 5. Generate unique Order Number
        SET @OrderNumber = 'ORD-' + CONVERT(NVARCHAR(8), GETUTCDATE(), 112) + '-' + SUBSTRING(CONVERT(NVARCHAR(36), NEWID()), 1, 8);

        -- 6. Insert Order Header
        INSERT INTO dbo.Orders (CustomerId, OrderNumber, TotalAmount, Status, CreatedAt)
        VALUES (@CustomerId, @OrderNumber, @TotalAmount, 'Completed', SYSUTCDATETIME());

        SET @NewOrderId = SCOPE_IDENTITY();

        -- 7. Insert Order Items from TVP
        INSERT INTO dbo.OrderItems (OrderId, ProductId, Quantity, UnitPrice)
        SELECT @NewOrderId, ProductId, Quantity, UnitPrice
        FROM @Items;

        -- 8. Decrement Stock Quantities atomically
        UPDATE p
        SET p.StockQuantity = p.StockQuantity - i.Quantity,
            p.UpdatedAt = SYSUTCDATETIME()
        FROM dbo.Products p
        INNER JOIN @Items i ON p.ProductId = i.ProductId;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        THROW;
    END CATCH
END;
GO

-- 7. Get Order By Id (Header & Line Items)
CREATE OR ALTER PROCEDURE dbo.sp_GetOrderById
    @OrderId INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: Order Header & Customer Info
    SELECT 
        o.OrderId,
        o.OrderNumber,
        o.CustomerId,
        c.FullName AS CustomerName,
        c.Email AS CustomerEmail,
        o.TotalAmount,
        o.Status,
        o.CreatedAt
    FROM dbo.Orders o
    INNER JOIN dbo.Customers c ON o.CustomerId = c.CustomerId
    WHERE o.OrderId = @OrderId;

    -- Result Set 2: Order Items
    SELECT 
        oi.OrderItemId,
        oi.OrderId,
        oi.ProductId,
        p.Name AS ProductName,
        p.Sku AS ProductSku,
        oi.Quantity,
        oi.UnitPrice,
        oi.TotalPrice
    FROM dbo.OrderItems oi
    INNER JOIN dbo.Products p ON oi.ProductId = p.ProductId
    WHERE oi.OrderId = @OrderId;
END;
GO

-- 8. Dashboard Analytics (Multiple Result Sets in a single trip)
CREATE OR ALTER PROCEDURE dbo.sp_GetDashboardSummary
AS
BEGIN
    SET NOCOUNT ON;

    -- Result Set 1: High Level KPI Metrics
    SELECT 
        (SELECT COUNT(1) FROM dbo.Products WHERE IsActive = 1) AS TotalActiveProducts,
        (SELECT ISNULL(COUNT(1), 0) FROM dbo.Orders) AS TotalOrdersCount,
        (SELECT ISNULL(SUM(TotalAmount), 0) FROM dbo.Orders) AS TotalRevenue,
        (SELECT COUNT(1) FROM dbo.Products WHERE IsActive = 1 AND StockQuantity < 10) AS LowStockProductsCount;

    -- Result Set 2: Top 5 Best Selling Products
    SELECT TOP 5
        p.ProductId,
        p.Name AS ProductName,
        p.Sku,
        SUM(oi.Quantity) AS UnitsSold,
        SUM(oi.TotalPrice) AS TotalSalesRevenue
    FROM dbo.OrderItems oi
    INNER JOIN dbo.Products p ON oi.ProductId = p.ProductId
    GROUP BY p.ProductId, p.Name, p.Sku
    ORDER BY UnitsSold DESC;

    -- Result Set 3: Low Stock Alerts (Stock < 10)
    SELECT 
        p.ProductId,
        p.Sku,
        p.Name AS ProductName,
        c.Name AS CategoryName,
        p.StockQuantity,
        p.Price
    FROM dbo.Products p
    INNER JOIN dbo.Categories c ON p.CategoryId = c.CategoryId
    WHERE p.IsActive = 1 AND p.StockQuantity < 10
    ORDER BY p.StockQuantity ASC;
END;
GO

