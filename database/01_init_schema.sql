-- ============================================================================
-- InventoryHub: Schema Initialization Script
-- Target: Microsoft SQL Server 2017+ / Azure SQL
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'InventoryDb')
BEGIN
    CREATE DATABASE InventoryDb;
END
GO

USE InventoryDb;
GO

-- Drop tables in reverse dependency order if recreating
IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U') IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL DROP TABLE dbo.Products;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Customers', 'U') IS NOT NULL DROP TABLE dbo.Customers;
IF TYPE_ID('dbo.OrderItemTableType') IS NOT NULL DROP TYPE dbo.OrderItemTableType;
GO

-- 1. Categories Table
CREATE TABLE dbo.Categories (
    CategoryId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(250) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- 2. Products Table
CREATE TABLE dbo.Products (
    ProductId INT IDENTITY(1,1) PRIMARY KEY,
    CategoryId INT NOT NULL,
    Sku NVARCHAR(50) NOT NULL UNIQUE,
    Name NVARCHAR(150) NOT NULL,
    Description NVARCHAR(500) NULL,
    Price DECIMAL(18, 2) NOT NULL CHECK (Price >= 0),
    StockQuantity INT NOT NULL DEFAULT 0 CHECK (StockQuantity >= 0),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt DATETIME2 NULL,
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(CategoryId)
);
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON dbo.Products(CategoryId);
CREATE NONCLUSTERED INDEX IX_Products_IsActive_Name ON dbo.Products(IsActive, Name);
GO

-- 3. Customers Table
CREATE TABLE dbo.Customers (
    CustomerId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL UNIQUE,
    Phone NVARCHAR(30) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);
GO

-- 4. Orders Table
CREATE TABLE dbo.Orders (
    OrderId INT IDENTITY(1,1) PRIMARY KEY,
    CustomerId INT NOT NULL,
    OrderNumber NVARCHAR(50) NOT NULL UNIQUE,
    TotalAmount DECIMAL(18, 2) NOT NULL DEFAULT 0,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Completed',
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId)
);
CREATE NONCLUSTERED INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
CREATE NONCLUSTERED INDEX IX_Orders_CreatedAt ON dbo.Orders(CreatedAt DESC);
GO

-- 5. OrderItems Table
CREATE TABLE dbo.OrderItems (
    OrderItemId INT IDENTITY(1,1) PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL CHECK (Quantity > 0),
    UnitPrice DECIMAL(18, 2) NOT NULL CHECK (UnitPrice >= 0),
    TotalPrice AS (Quantity * UnitPrice) PERSISTED,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(OrderId) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products(ProductId)
);
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
GO

-- 6. Table Valued Parameter (TVP) for bulk item insertion
CREATE TYPE dbo.OrderItemTableType AS TABLE (
    ProductId INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL
);
GO

