IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[Stores]')
    AND name = 'IsExternal'
)
BEGIN
    ALTER TABLE [Stores]
    ADD [IsExternal] bit NOT NULL DEFAULT 0;
END
