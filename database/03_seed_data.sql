-- ============================================================================
-- InventoryHub: Initial Seed Data
-- ============================================================================

USE InventoryDb;
GO

-- 1. Seed Categories
IF NOT EXISTS (SELECT 1 FROM dbo.Categories)
BEGIN
    INSERT INTO dbo.Categories (Name, Description)
    VALUES 
        ('Electronics', 'Smartphones, laptops, components and gadgets'),
        ('Office Supplies', 'Ergonomic chairs, desks, stationery and accessories'),
        ('Networking', 'Routers, switches, cables, and enterprise equipment'),
        ('Accessories', 'Chargers, cases, keyboards, mice, and peripherals');
END
GO

-- 2. Seed Customers
IF NOT EXISTS (SELECT 1 FROM dbo.Customers)
BEGIN
    INSERT INTO dbo.Customers (FullName, Email, Phone)
    VALUES 
        ('Ekrem Cakir', 'ekrem@example.com', '+90 555 123 4567'),
        ('John Doe', 'john.doe@example.com', '+1 555 987 6543'),
        ('Jane Smith', 'jane.smith@example.com', '+1 555 333 4444');
END
GO

-- 3. Seed Products
IF NOT EXISTS (SELECT 1 FROM dbo.Products)
BEGIN
    DECLARE @CatElectronics INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Electronics');
    DECLARE @CatOffice INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Office Supplies');
    DECLARE @CatNetworking INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Networking');
    DECLARE @CatAccessories INT = (SELECT CategoryId FROM dbo.Categories WHERE Name = 'Accessories');

    INSERT INTO dbo.Products (CategoryId, Sku, Name, Description, Price, StockQuantity, IsActive)
    VALUES 
        (@CatElectronics, 'ELEC-MBP-14', 'MacBook Pro 14" M3', 'Apple MacBook Pro 14-inch with M3 chip, 16GB RAM, 512GB SSD', 1999.00, 25, 1),
        (@CatElectronics, 'ELEC-DELL-XPS', 'Dell XPS 15', 'Dell XPS 15-inch Intel i7, 32GB RAM, 1TB SSD', 1749.50, 15, 1),
        (@CatAccessories, 'ACC-MX-M3S', 'Logitech MX Master 3S', 'Ergonomic wireless performance mouse', 99.99, 45, 1),
        (@CatAccessories, 'ACC-KCH-PRO', 'Keychron K2 Mechanical Keyboard', 'Wireless Bluetooth 75% Layout RGB backlit keyboard', 89.00, 8, 1), -- Low stock sample
        (@CatNetworking, 'NET-UBQ-U6', 'Ubiquiti UniFi 6 Pro', 'High-performance WiFi 6 dual-band ceiling access point', 159.00, 12, 1),
        (@CatOffice, 'OFF-HM-AER', 'Herman Miller Aeron Chair', 'Remastered ergonomic office chair with posturefit support', 1195.00, 5, 1); -- Low stock sample
END
GO

-- 4. Seed a Sample Order using the Stored Procedure
IF NOT EXISTS (SELECT 1 FROM dbo.Orders)
BEGIN
    DECLARE @CustomerId INT = (SELECT TOP 1 CustomerId FROM dbo.Customers);
    DECLARE @Product1Id INT = (SELECT ProductId FROM dbo.Products WHERE Sku = 'ACC-MX-M3S');
    DECLARE @Product2Id INT = (SELECT ProductId FROM dbo.Products WHERE Sku = 'ELEC-MBP-14');

    DECLARE @Items dbo.OrderItemTableType;
    INSERT INTO @Items (ProductId, Quantity, UnitPrice)
    VALUES 
        (@Product1Id, 2, 99.99),
        (@Product2Id, 1, 1999.00);

    DECLARE @NewOrderId INT;
    DECLARE @OrderNumber NVARCHAR(50);

    EXEC dbo.sp_CreateOrderWithItems
        @CustomerId = @CustomerId,
        @Items = @Items,
        @NewOrderId = @NewOrderId OUTPUT,
        @OrderNumber = @OrderNumber OUTPUT;
END
GO

