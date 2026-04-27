IF COL_LENGTH('Products', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Products ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Products_IsDeleted DEFAULT 0;
    CREATE INDEX IX_Products_IsDeleted ON Products(IsDeleted);
END

IF COL_LENGTH('Categories', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Categories ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Categories_IsDeleted DEFAULT 0;
    CREATE INDEX IX_Categories_IsDeleted ON Categories(IsDeleted);
END

IF COL_LENGTH('Stores', 'IsDeleted') IS NULL
BEGIN
    ALTER TABLE Stores ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Stores_IsDeleted DEFAULT 0;
    CREATE INDEX IX_Stores_IsDeleted ON Stores(IsDeleted);
END
