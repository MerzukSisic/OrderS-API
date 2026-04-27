IF COL_LENGTH('Orders', 'IsArchived') IS NULL
BEGIN
    ALTER TABLE Orders ADD IsArchived BIT NOT NULL CONSTRAINT DF_Orders_IsArchived DEFAULT 0;
    CREATE INDEX IX_Orders_IsArchived ON Orders(IsArchived);
END

IF COL_LENGTH('Orders', 'ArchivedAt') IS NULL
BEGIN
    ALTER TABLE Orders ADD ArchivedAt DATETIME2 NULL;
END
