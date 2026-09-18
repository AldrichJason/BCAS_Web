/* =============================================================================
   BCAS Web Portal & CMS - Session termination and password resets
   BW-11: revoked access tokens (server-side logout)
   BW-13: password reset tokens

   Idempotent and repeatable. Run after 001_schema.sql.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -----------------------------------------------------------------------------
   Revoked access tokens (BW-11)

   Access tokens are stateless, so logout records the token's JWT id here and
   the API rejects any token whose jti is listed. Rows are only useful until the
   token would have expired anyway; purge with ops.PurgeExpiredAuthTokens.
   -------------------------------------------------------------------------- */
IF OBJECT_ID('auth.RevokedTokens', 'U') IS NULL
BEGIN
    CREATE TABLE auth.RevokedTokens
    (
        Jti        CHAR(32)      NOT NULL,   -- JWT "jti" claim, Guid formatted as "N"
        UserId     INT           NOT NULL,
        -- The token's own expiry: once past, the row can be purged because the
        -- token is rejected on lifetime alone.
        ExpiresAt  DATETIME2(3)  NOT NULL,
        RevokedAt  DATETIME2(3)  NOT NULL CONSTRAINT DF_RevokedTokens_RevokedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_RevokedTokens PRIMARY KEY CLUSTERED (Jti),
        CONSTRAINT FK_RevokedTokens_Users FOREIGN KEY (UserId) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_RevokedTokens_ExpiresAt ON auth.RevokedTokens (ExpiresAt);
    CREATE INDEX IX_RevokedTokens_UserId ON auth.RevokedTokens (UserId);
END
GO

/* -----------------------------------------------------------------------------
   Password reset tokens (BW-13)

   Only the SHA-256 hash of the token is stored, so a leaked table cannot be
   used to reset anyone's password. ConsumedAt makes a token single-use.
   -------------------------------------------------------------------------- */
IF OBJECT_ID('auth.PasswordResetTokens', 'U') IS NULL
BEGIN
    CREATE TABLE auth.PasswordResetTokens
    (
        TokenId     INT           IDENTITY(1,1) NOT NULL,
        UserId      INT           NOT NULL,
        -- Base64 of SHA-256 over the raw token. The raw token only ever exists
        -- in the reset email.
        TokenHash   CHAR(44)      NOT NULL,
        ExpiresAt   DATETIME2(3)  NOT NULL,
        ConsumedAt  DATETIME2(3)  NULL,
        -- Set when a newer request or a successful reset supersedes this token.
        InvalidatedAt DATETIME2(3) NULL,
        CreatedAt   DATETIME2(3)  NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        RequestedIp VARCHAR(45)   NULL,
        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY CLUSTERED (TokenId),
        CONSTRAINT UQ_PasswordResetTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_PasswordResetTokens_UserId ON auth.PasswordResetTokens (UserId, ExpiresAt DESC);
    CREATE INDEX IX_PasswordResetTokens_ExpiresAt ON auth.PasswordResetTokens (ExpiresAt);
END
GO

/* -----------------------------------------------------------------------------
   Housekeeping: drop rows that can no longer affect an authentication decision.
   Safe to run on a schedule (SQL Agent job or a hosted service).
   -------------------------------------------------------------------------- */
CREATE OR ALTER PROCEDURE ops.PurgeExpiredAuthTokens
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM auth.RevokedTokens
    WHERE ExpiresAt < SYSUTCDATETIME();

    DELETE FROM auth.PasswordResetTokens
    WHERE ExpiresAt < DATEADD(DAY, -7, SYSUTCDATETIME());
END;
GO

PRINT 'BCAS session and password-reset tables applied.';
GO
