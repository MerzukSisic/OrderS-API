IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[ProcurementOrders]')
    AND name = 'SourceStoreId'
)
BEGIN
    ALTER TABLE [ProcurementOrders]
    ADD [SourceStoreId] uniqueidentifier NULL;
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_ProcurementOrders_Stores_SourceStoreId'
)
BEGIN
    ALTER TABLE [ProcurementOrders]
    ADD CONSTRAINT [FK_ProcurementOrders_Stores_SourceStoreId]
    FOREIGN KEY ([SourceStoreId]) REFERENCES [Stores]([Id]);
END
