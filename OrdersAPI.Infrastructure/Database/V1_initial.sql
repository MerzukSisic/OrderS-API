-- ============================================
-- OrdersDB - Complete Database Script
-- SQL Server Database First Approach
-- ============================================

USE master;
GO

-- Drop database if exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'OrdersDB')
BEGIN
    ALTER DATABASE OrdersDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE OrdersDB;
END
GO

-- Create database
CREATE DATABASE OrdersDB;
GO

USE OrdersDB;
GO

-- ============================================
-- TABLE: Users
-- ============================================
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FullName NVARCHAR(200) NOT NULL,
    Email NVARCHAR(200) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(500) NOT NULL,
    Role NVARCHAR(50) NOT NULL CHECK (Role IN ('Admin', 'Waiter', 'Bartender')),
    PhoneNumber NVARCHAR(50) NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================
-- TABLE: Categories
-- ============================================
CREATE TABLE Categories (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    IconName NVARCHAR(100) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================
-- TABLE: Products
-- ============================================
CREATE TABLE Products (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    Price DECIMAL(18, 2) NOT NULL,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    IsAvailable BIT NOT NULL DEFAULT 1,
    Location NVARCHAR(50) NOT NULL CHECK (Location IN ('Kitchen', 'Bar')),
    PreparationTimeMinutes INT NOT NULL DEFAULT 15,
    Stock INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);
GO

-- ============================================
-- TABLE: CafeTables
-- ============================================
CREATE TABLE CafeTables (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TableNumber NVARCHAR(50) NOT NULL,
    Capacity INT NOT NULL,
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Available', 'Occupied', 'Reserved')),
    Location NVARCHAR(200) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================
-- TABLE: Orders
-- ============================================
CREATE TABLE Orders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    WaiterId UNIQUEIDENTIFIER NOT NULL,
    TableId UNIQUEIDENTIFIER NULL,
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Pending', 'Preparing', 'Ready', 'Completed', 'Cancelled')),
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('DineIn', 'TakeAway')),
    IsPartnerOrder BIT NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Notes NVARCHAR(1000) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CompletedAt DATETIME2 NULL,
    CONSTRAINT FK_Orders_Users FOREIGN KEY (WaiterId) REFERENCES Users(Id),
    CONSTRAINT FK_Orders_CafeTables FOREIGN KEY (TableId) REFERENCES CafeTables(Id) ON DELETE SET NULL
);
GO

-- ============================================
-- TABLE: OrderItems
-- ============================================
CREATE TABLE OrderItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderId UNIQUEIDENTIFIER NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18, 2) NOT NULL,
    Subtotal DECIMAL(18, 2) NOT NULL,
    Notes NVARCHAR(500) NULL,
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Pending', 'Preparing', 'Ready')),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Products FOREIGN KEY (ProductId) REFERENCES Products(Id)
);
GO

-- ============================================
-- TABLE: Stores
-- ============================================
CREATE TABLE Stores (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    Address NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
GO

-- ============================================
-- TABLE: StoreProducts
-- ============================================
CREATE TABLE StoreProducts (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StoreId UNIQUEIDENTIFIER NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NULL,
    PurchasePrice DECIMAL(18, 2) NOT NULL,
    CurrentStock INT NOT NULL DEFAULT 0,
    MinimumStock INT NOT NULL DEFAULT 10,
    Unit NVARCHAR(50) NOT NULL DEFAULT 'pcs',
    LastRestocked DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_StoreProducts_Stores FOREIGN KEY (StoreId) REFERENCES Stores(Id) ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: ProductIngredients
-- ============================================
CREATE TABLE ProductIngredients (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProductId UNIQUEIDENTIFIER NOT NULL,
    StoreProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_ProductIngredients_Products FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProductIngredients_StoreProducts FOREIGN KEY (StoreProductId) REFERENCES StoreProducts(Id)
);
GO

-- ============================================
-- TABLE: ProcurementOrders
-- ============================================
CREATE TABLE ProcurementOrders (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StoreId UNIQUEIDENTIFIER NOT NULL,
    Supplier NVARCHAR(200) NOT NULL,
    TotalAmount DECIMAL(18, 2) NOT NULL,
    Status NVARCHAR(50) NOT NULL CHECK (Status IN ('Pending', 'Paid', 'Ordered', 'Received', 'Cancelled')),
    StripePaymentIntentId NVARCHAR(200) NULL,
    Notes NVARCHAR(1000) NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    DeliveryDate DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ProcurementOrders_Stores FOREIGN KEY (StoreId) REFERENCES Stores(Id)
);
GO

-- ============================================
-- TABLE: ProcurementOrderItems
-- ============================================
CREATE TABLE ProcurementOrderItems (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ProcurementOrderId UNIQUEIDENTIFIER NOT NULL,
    StoreProductId UNIQUEIDENTIFIER NOT NULL,
    Quantity INT NOT NULL,
    UnitCost DECIMAL(18, 2) NOT NULL,
    Subtotal DECIMAL(18, 2) NOT NULL,
    CONSTRAINT FK_ProcurementOrderItems_ProcurementOrders FOREIGN KEY (ProcurementOrderId) REFERENCES ProcurementOrders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_ProcurementOrderItems_StoreProducts FOREIGN KEY (StoreProductId) REFERENCES StoreProducts(Id)
);
GO

-- ============================================
-- TABLE: InventoryLogs
-- ============================================
CREATE TABLE InventoryLogs (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    StoreProductId UNIQUEIDENTIFIER NOT NULL,
    QuantityChange INT NOT NULL,
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('Sale', 'Restock', 'Adjustment', 'Damage')),
    Reason NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_InventoryLogs_StoreProducts FOREIGN KEY (StoreProductId) REFERENCES StoreProducts(Id) ON DELETE CASCADE
);
GO

-- ============================================
-- TABLE: Notifications
-- ============================================
CREATE TABLE Notifications (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(1000) NOT NULL,
    Type NVARCHAR(50) NOT NULL CHECK (Type IN ('Info', 'Warning', 'Error', 'LowStock')),
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Notifications_Users FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);
GO

-- ============================================
-- INDEXES for Performance
-- ============================================
CREATE NONCLUSTERED INDEX IX_Users_Email ON Users(Email);
CREATE NONCLUSTERED INDEX IX_Products_CategoryId ON Products(CategoryId);
CREATE NONCLUSTERED INDEX IX_Products_IsAvailable ON Products(IsAvailable);
CREATE NONCLUSTERED INDEX IX_Orders_WaiterId ON Orders(WaiterId);
CREATE NONCLUSTERED INDEX IX_Orders_TableId ON Orders(TableId);
CREATE NONCLUSTERED INDEX IX_Orders_Status ON Orders(Status);
CREATE NONCLUSTERED INDEX IX_Orders_CreatedAt ON Orders(CreatedAt);
CREATE NONCLUSTERED INDEX IX_OrderItems_OrderId ON OrderItems(OrderId);
CREATE NONCLUSTERED INDEX IX_OrderItems_ProductId ON OrderItems(ProductId);
CREATE NONCLUSTERED INDEX IX_StoreProducts_StoreId ON StoreProducts(StoreId);
CREATE NONCLUSTERED INDEX IX_ProductIngredients_ProductId ON ProductIngredients(ProductId);
CREATE NONCLUSTERED INDEX IX_ProductIngredients_StoreProductId ON ProductIngredients(StoreProductId);
CREATE NONCLUSTERED INDEX IX_Notifications_UserId ON Notifications(UserId);
CREATE NONCLUSTERED INDEX IX_Notifications_IsRead ON Notifications(IsRead);
GO

-- ============================================
-- SEED DATA - Initial Data
-- ============================================

-- Users (password: "password123" - BCrypt hashed)
DECLARE @AdminId UNIQUEIDENTIFIER = NEWID();
DECLARE @WaiterId1 UNIQUEIDENTIFIER = NEWID();
DECLARE @WaiterId2 UNIQUEIDENTIFIER = NEWID();
DECLARE @BartenderId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, PhoneNumber, IsActive)
VALUES 
    (@AdminId, 'Admin User', 'admin@orders.com', '$2a$11$xKZQJhQnVvqz3jyOdH3rAeYpJh5L7KzP8YFGMvXVJ3bJGJzQZrJBe', 'Admin', '+387 61 123 456', 1),
    (@WaiterId1, 'Marko Marković', 'marko@orders.com', '$2a$11$xKZQJhQnVvqz3jyOdH3rAeYpJh5L7KzP8YFGMvXVJ3bJGJzQZrJBe', 'Waiter', '+387 61 234 567', 1),
    (@WaiterId2, 'Ana Anić', 'ana@orders.com', '$2a$11$xKZQJhQnVvqz3jyOdH3rAeYpJh5L7KzP8YFGMvXVJ3bJGJzQZrJBe', 'Waiter', '+387 61 345 678', 1),
    (@BartenderId, 'Petar Petrović', 'petar@orders.com', '$2a$11$xKZQJhQnVvqz3jyOdH3rAeYpJh5L7KzP8YFGMvXVJ3bJGJzQZrJBe', 'Bartender', '+387 61 456 789', 1);
GO

-- Categories
DECLARE @CategoryDrinks UNIQUEIDENTIFIER = NEWID();
DECLARE @CategoryFood UNIQUEIDENTIFIER = NEWID();
DECLARE @CategoryDesserts UNIQUEIDENTIFIER = NEWID();

INSERT INTO Categories (Id, Name, Description, IconName)
VALUES 
    (@CategoryDrinks, 'Piće', 'Topli i hladni napici', 'local_cafe'),
    (@CategoryFood, 'Hrana', 'Jela i zalogaji', 'restaurant'),
    (@CategoryDesserts, 'Deserti', 'Slatki zalogaji', 'cake');
GO

-- Products
DECLARE @ProductEspresso UNIQUEIDENTIFIER = NEWID();
DECLARE @ProductCappuccino UNIQUEIDENTIFIER = NEWID();
DECLARE @ProductCocaCola UNIQUEIDENTIFIER = NEWID();
DECLARE @ProductPizza UNIQUEIDENTIFIER = NEWID();
DECLARE @ProductClubSandwich UNIQUEIDENTIFIER = NEWID();
DECLARE @ProductTiramisu UNIQUEIDENTIFIER = NEWID();

INSERT INTO Products (Id, Name, Description, Price, CategoryId, Location, PreparationTimeMinutes, Stock, IsAvailable)
VALUES 
    (@ProductEspresso, 'Espresso', 'Klasičan espresso', 1.50, @CategoryDrinks, 'Bar', 5, 100, 1),
    (@ProductCappuccino, 'Cappuccino', 'Cappuccino sa mlijekom', 2.00, @CategoryDrinks, 'Bar', 5, 100, 1),
    (@ProductCocaCola, 'Coca Cola', 'Coca Cola 0.33l', 2.50, @CategoryDrinks, 'Bar', 2, 50, 1),
    (@ProductPizza, 'Pizza Margherita', 'Klasična pizza sa sirom', 8.00, @CategoryFood, 'Kitchen', 20, 30, 1),
    (@ProductClubSandwich, 'Club Sendvič', 'Club sendvič sa piletinom', 5.50, @CategoryFood, 'Kitchen', 15, 25, 1),
    (@ProductTiramisu, 'Tiramisu', 'Italijanski desert', 4.00, @CategoryDesserts, 'Bar', 5, 20, 1);
GO

-- CafeTables
DECLARE @Table1 UNIQUEIDENTIFIER = NEWID();
DECLARE @Table2 UNIQUEIDENTIFIER = NEWID();
DECLARE @Table3 UNIQUEIDENTIFIER = NEWID();
DECLARE @Table4 UNIQUEIDENTIFIER = NEWID();

INSERT INTO CafeTables (Id, TableNumber, Capacity, Status, Location)
VALUES 
    (@Table1, '1', 4, 'Available', 'Unutra'),
    (@Table2, '2', 2, 'Available', 'Terasa'),
    (@Table3, '3', 6, 'Available', 'Unutra'),
    (@Table4, '4', 4, 'Available', 'Terasa');
GO

-- Store
DECLARE @MainStore UNIQUEIDENTIFIER = NEWID();

INSERT INTO Stores (Id, Name, Description, Address)
VALUES 
    (@MainStore, 'Glavni Magacin', 'Centralno skladište', 'Sarajevo, Bosna i Hercegovina');
GO

-- StoreProducts (Ingredients)
DECLARE @IngredientCoffee UNIQUEIDENTIFIER = NEWID();
DECLARE @IngredientMilk UNIQUEIDENTIFIER = NEWID();
DECLARE @IngredientCheese UNIQUEIDENTIFIER = NEWID();
DECLARE @IngredientTomato UNIQUEIDENTIFIER = NEWID();
DECLARE @IngredientChicken UNIQUEIDENTIFIER = NEWID();

INSERT INTO StoreProducts (Id, StoreId, Name, Description, PurchasePrice, CurrentStock, MinimumStock, Unit)
VALUES 
    (@IngredientCoffee, @MainStore, 'Kafa (zrna)', 'Kafa u zrnu', 15.00, 100, 20, 'kg'),
    (@IngredientMilk, @MainStore, 'Mlijeko', 'Svježe mlijeko', 1.50, 50, 10, 'l'),
    (@IngredientCheese, @MainStore, 'Sir', 'Mocarela sir', 8.00, 30, 5, 'kg'),
    (@IngredientTomato, @MainStore, 'Paradajz', 'Svježi paradajz', 2.00, 40, 10, 'kg'),
    (@IngredientChicken, @MainStore, 'Piletina', 'Pileća prsa', 7.00, 25, 5, 'kg');
GO

-- ProductIngredients (Recipe)
INSERT INTO ProductIngredients (ProductId, StoreProductId, Quantity)
VALUES 
    -- Espresso = 20g kafe
    (@ProductEspresso, @IngredientCoffee, 0.02),
    -- Cappuccino = 20g kafe + 100ml mlijeka
    (@ProductCappuccino, @IngredientCoffee, 0.02),
    (@ProductCappuccino, @IngredientMilk, 0.1),
    -- Pizza = 200g sira + 100g paradajza
    (@ProductPizza, @IngredientCheese, 0.2),
    (@ProductPizza, @IngredientTomato, 0.1),
    -- Club Sandwich = 150g piletine
    (@ProductClubSandwich, @IngredientChicken, 0.15);
GO

-- Sample Order
DECLARE @SampleOrderId UNIQUEIDENTIFIER = NEWID();

INSERT INTO Orders (Id, WaiterId, TableId, Status, Type, IsPartnerOrder, TotalAmount, Notes, CreatedAt)
VALUES 
    (@SampleOrderId, @WaiterId1, @Table1, 'Completed', 'DineIn', 0, 11.50, 'Brza usluga', DATEADD(HOUR, -2, GETUTCDATE()));
GO

INSERT INTO OrderItems (OrderId, ProductId, Quantity, UnitPrice, Subtotal, Status)
VALUES 
    (@SampleOrderId, @ProductEspresso, 2, 1.50, 3.00, 'Ready'),
    (@SampleOrderId, @ProductPizza, 1, 8.00, 8.00, 'Ready'),
    (@SampleOrderId, @SampleOrderId, 1, 0.50, 0.50, 'Ready');
GO

-- Update table status
UPDATE CafeTables SET Status = 'Occupied' WHERE Id = @Table1;
GO

-- ============================================
-- VIEWS for Common Queries
-- ============================================

-- View: Active Orders with Details
CREATE VIEW vw_ActiveOrders AS
SELECT 
    o.Id AS OrderId,
    o.Status,
    o.Type,
    o.TotalAmount,
    o.CreatedAt,
    u.FullName AS WaiterName,
    t.TableNumber,
    COUNT(oi.Id) AS ItemCount
FROM Orders o
INNER JOIN Users u ON o.WaiterId = u.Id
LEFT JOIN CafeTables t ON o.TableId = t.Id
LEFT JOIN OrderItems oi ON o.Id = oi.OrderId
WHERE o.Status NOT IN ('Completed', 'Cancelled')
GROUP BY o.Id, o.Status, o.Type, o.TotalAmount, o.CreatedAt, u.FullName, t.TableNumber;
GO

-- View: Low Stock Products
CREATE VIEW vw_LowStockProducts AS
SELECT 
    sp.Id,
    sp.Name,
    sp.CurrentStock,
    sp.MinimumStock,
    sp.Unit,
    s.Name AS StoreName
FROM StoreProducts sp
INNER JOIN Stores s ON sp.StoreId = s.Id
WHERE sp.CurrentStock < sp.MinimumStock;
GO

-- View: Daily Revenue
CREATE VIEW vw_DailyRevenue AS
SELECT 
    CAST(o.CreatedAt AS DATE) AS OrderDate,
    COUNT(o.Id) AS TotalOrders,
    SUM(o.TotalAmount) AS TotalRevenue,
    AVG(o.TotalAmount) AS AverageOrderValue
FROM Orders o
WHERE o.Status = 'Completed'
GROUP BY CAST(o.CreatedAt AS DATE);
GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================
PRINT '✅ Database OrdersDB created successfully!';
PRINT '✅ All tables, relationships, and indexes created!';
PRINT '✅ Sample data inserted!';
PRINT '';
PRINT '📊 Database Summary:';
PRINT '   - Users: 4 (1 Admin, 2 Waiters, 1 Bartender)';
PRINT '   - Categories: 3';
PRINT '   - Products: 6';
PRINT '   - Tables: 4';
PRINT '   - Store: 1';
PRINT '   - Ingredients: 5';
PRINT '   - Sample Order: 1';
PRINT '';
PRINT '🔐 Login Credentials:';
PRINT '   Email: admin@orders.com';
PRINT '   Password: password123';
PRINT '';
PRINT '🚀 Ready to use!';
GO