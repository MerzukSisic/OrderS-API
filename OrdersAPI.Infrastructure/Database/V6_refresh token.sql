-- ============================================================
-- Schema Update: Add RefreshTokens and PasswordResetTokens
-- Run this script against an existing database.
-- Safe to run more than once (IF NOT EXISTS guards).
-- ============================================================

-- RefreshTokens table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'RefreshTokens'
)
BEGIN
    CREATE TABLE RefreshTokens (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserId      UNIQUEIDENTIFIER NOT NULL,
        TokenHash   NVARCHAR(512)    NOT NULL,
        ExpiresAt   DATETIME2        NOT NULL,
        IsRevoked   BIT              NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2        NOT NULL,
        RevokedAt   DATETIME2        NULL,

        CONSTRAINT FK_RefreshTokens_Users
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RefreshTokens_TokenHash ON RefreshTokens(TokenHash);
    CREATE INDEX IX_RefreshTokens_UserId    ON RefreshTokens(UserId);

    PRINT 'Created table RefreshTokens';
END
ELSE
BEGIN
    PRINT 'Table RefreshTokens already exists — skipped';
END

-- PasswordResetTokens table
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES
    WHERE TABLE_NAME = 'PasswordResetTokens'
)
BEGIN
    CREATE TABLE PasswordResetTokens (
        Id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserId      UNIQUEIDENTIFIER NOT NULL,
        TokenHash   NVARCHAR(512)    NOT NULL,
        ExpiresAt   DATETIME2        NOT NULL,
        IsUsed      BIT              NOT NULL DEFAULT 0,
        CreatedAt   DATETIME2        NOT NULL,

        CONSTRAINT FK_PasswordResetTokens_Users
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_PasswordResetTokens_TokenHash ON PasswordResetTokens(TokenHash);
    CREATE INDEX IX_PasswordResetTokens_UserId    ON PasswordResetTokens(UserId);

    PRINT 'Created table PasswordResetTokens';
END
ELSE
BEGIN
    PRINT 'Table PasswordResetTokens already exists — skipped';
END
