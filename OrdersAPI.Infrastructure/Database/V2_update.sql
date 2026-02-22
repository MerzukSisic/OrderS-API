-- =============================================
-- Migration: Add Accompaniment System
-- Description: Adds support for product accompaniments (side dishes, vegetables, etc.)
-- Date: 2024-12-28
-- =============================================

-- AccompanimentGroups Table
CREATE TABLE AccompanimentGroups (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    ProductId UNIQUEIDENTIFIER NOT NULL,
    SelectionType NVARCHAR(50) NOT NULL, -- 'Single' ili 'Multiple'
    IsRequired BIT NOT NULL DEFAULT 0,
    MinSelections INT NULL,
    MaxSelections INT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_AccompanimentGroups_Products 
        FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE CASCADE
);

-- Accompaniments Table
CREATE TABLE Accompaniments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Name NVARCHAR(100) NOT NULL,
    ExtraCharge DECIMAL(18,2) NOT NULL DEFAULT 0,
    AccompanimentGroupId UNIQUEIDENTIFIER NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsAvailable BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_Accompaniments_AccompanimentGroups 
        FOREIGN KEY (AccompanimentGroupId) REFERENCES AccompanimentGroups(Id) ON DELETE CASCADE
);

-- OrderItemAccompaniments Table (Junction table)
CREATE TABLE OrderItemAccompaniments (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OrderItemId UNIQUEIDENTIFIER NOT NULL,
    AccompanimentId UNIQUEIDENTIFIER NOT NULL,
    PriceAtOrder DECIMAL(18,2) NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    
    CONSTRAINT FK_OrderItemAccompaniments_OrderItems 
        FOREIGN KEY (OrderItemId) REFERENCES OrderItems(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItemAccompaniments_Accompaniments 
        FOREIGN KEY (AccompanimentId) REFERENCES Accompaniments(Id) ON DELETE NO ACTION
);

-- Indexes for better performance
CREATE INDEX IX_AccompanimentGroups_ProductId ON AccompanimentGroups(ProductId);
CREATE INDEX IX_Accompaniments_AccompanimentGroupId ON Accompaniments(AccompanimentGroupId);
CREATE INDEX IX_OrderItemAccompaniments_OrderItemId ON OrderItemAccompaniments(OrderItemId);
CREATE INDEX IX_OrderItemAccompaniments_AccompanimentId ON OrderItemAccompaniments(AccompanimentId);

GO

-- =============================================
-- Sample Data: Primjeri za testiranje
-- =============================================

-- Napomena: Zamijeni ove GUID-ove sa stvarnim ID-evima iz tvoje baze
-- DECLARE @CevapiId UNIQUEIDENTIFIER = (SELECT Id FROM Products WHERE Name = 'Ćevapi');
-- DECLARE @FiletId UNIQUEIDENTIFIER = (SELECT Id FROM Products WHERE Name = 'Piletina Filet');
-- DECLARE @KuhanoId UNIQUEIDENTIFIER = (SELECT Id FROM Products WHERE Name = 'Punjene Paprike');

-- Primjer 1: Ćevapi - Dodatci (Multiple selection)
-- DECLARE @CevapiDodaciGroupId UNIQUEIDENTIFIER = NEWID();
-- INSERT INTO AccompanimentGroups (Id, Name, ProductId, SelectionType, IsRequired, MinSelections, MaxSelections, DisplayOrder)
-- VALUES (@CevapiDodaciGroupId, 'Dodatci', @CevapiId, 'Multiple', 0, NULL, NULL, 1);

-- INSERT INTO Accompaniments (Name, ExtraCharge, AccompanimentGroupId, DisplayOrder)
-- VALUES 
--     ('Sve', 0.50, @CevapiDodaciGroupId, 1),
--     ('Kupus', 0.30, @CevapiDodaciGroupId, 2),
--     ('Krastavice', 0.30, @CevapiDodaciGroupId, 3),
--     ('Paradajz', 0.30, @CevapiDodaciGroupId, 4),
--     ('Luk', 0.20, @CevapiDodaciGroupId, 5);

-- Primjer 2: Filet - Garnitura (Single selection, Required)
-- DECLARE @FiletGarnituraGroupId UNIQUEIDENTIFIER = NEWID();
-- INSERT INTO AccompanimentGroups (Id, Name, ProductId, SelectionType, IsRequired, MinSelections, MaxSelections, DisplayOrder)
-- VALUES (@FiletGarnituraGroupId, 'Garnitura', @FiletId, 'Single', 1, 1, 1, 1);

-- INSERT INTO Accompaniments (Name, ExtraCharge, AccompanimentGroupId, DisplayOrder)
-- VALUES 
--     ('Pomfrit', 0, @FiletGarnituraGroupId, 1),
--     ('Riža', 0, @FiletGarnituraGroupId, 2),
--     ('Bez garniture', 0, @FiletGarnituraGroupId, 3);

-- Primjer 3: Kuhano jelo - Prilog (Single selection, Optional)
-- DECLARE @KuhanoPrilogGroupId UNIQUEIDENTIFIER = NEWID();
-- INSERT INTO AccompanimentGroups (Id, Name, ProductId, SelectionType, IsRequired, MinSelections, MaxSelections, DisplayOrder)
-- VALUES (@KuhanoPrilogGroupId, 'Prilog', @KuhanoId, 'Single', 0, NULL, 1, 1);

-- INSERT INTO Accompaniments (Name, ExtraCharge, AccompanimentGroupId, DisplayOrder)
-- VALUES 
--     ('Pire krompir', 0, @KuhanoPrilogGroupId, 1),
--     ('Riža', 0, @KuhanoPrilogGroupId, 2),
--     ('Bez priloga', 0, @KuhanoPrilogGroupId, 3);

PRINT 'Migration completed successfully!';