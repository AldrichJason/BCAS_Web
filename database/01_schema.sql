/* =============================================================================
   BCAS Web Portal - schema
   Target: SQL Server 2019 or newer
   Usage : sqlcmd -S localhost,1433 -U sa -P <password> -i 01_schema.sql
   The script is idempotent: it can be re-run on an existing database.
   ============================================================================= */

IF DB_ID('BCAS_Web') IS NULL
BEGIN
    CREATE DATABASE BCAS_Web;
END
GO

USE BCAS_Web;
GO

/* ---------------------------------------------------------------- Roles ---- */
IF OBJECT_ID('dbo.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id          INT             IDENTITY(1,1) NOT NULL,
        Name        NVARCHAR(50)    NOT NULL,
        Description NVARCHAR(200)   NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Roles_Name UNIQUE (Name)
    );
END
GO

/* ---------------------------------------------------------------- Users ---- */
IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id           INT            IDENTITY(1,1) NOT NULL,
        Email        NVARCHAR(256)  NOT NULL,
        PasswordHash NVARCHAR(200)  NOT NULL,
        FullName     NVARCHAR(150)  NOT NULL,
        RoleId       INT            NOT NULL,
        IsActive     BIT            NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Users_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id)
    );
END
GO

/* -------------------------------------------------------- RefreshTokens ---- */
IF OBJECT_ID('dbo.RefreshTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id        BIGINT       IDENTITY(1,1) NOT NULL,
        UserId    INT          NOT NULL,
        -- SHA-256 of the token handed to the client; the token itself is never stored.
        TokenHash CHAR(64)     NOT NULL,
        ExpiresAt DATETIME2(3) NOT NULL,
        CreatedAt DATETIME2(3) NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        RevokedAt DATETIME2(3) NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_RefreshTokens_TokenHash UNIQUE (TokenHash),
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId) INCLUDE (ExpiresAt, RevokedAt);
END
GO

/* -------------------------------------------------------- Announcements ---- */
IF OBJECT_ID('dbo.Announcements', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Announcements
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Title       NVARCHAR(200)  NOT NULL,
        Slug        NVARCHAR(220)  NOT NULL,
        Summary     NVARCHAR(500)  NOT NULL,
        Content     NVARCHAR(MAX)  NOT NULL,
        ImageUrl    NVARCHAR(500)  NULL,
        Category    NVARCHAR(80)   NOT NULL,
        IsPublished BIT            NOT NULL CONSTRAINT DF_Announcements_IsPublished DEFAULT (0),
        PublishedAt DATETIME2(3)   NULL,
        AuthorId    INT            NOT NULL,
        CreatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Announcements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_Announcements_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Announcements PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Announcements_Slug UNIQUE (Slug),
        CONSTRAINT FK_Announcements_Users FOREIGN KEY (AuthorId) REFERENCES dbo.Users (Id)
    );

    -- Supports the public listing: published items, newest first.
    CREATE INDEX IX_Announcements_Published
        ON dbo.Announcements (IsPublished, PublishedAt DESC)
        INCLUDE (Title, Slug, Summary, Category);

    CREATE INDEX IX_Announcements_Category ON dbo.Announcements (Category);
END
GO

/* ------------------------------------------------------------- Programs ---- */
IF OBJECT_ID('dbo.Programs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Programs
    (
        Id            INT            IDENTITY(1,1) NOT NULL,
        Code          NVARCHAR(20)   NOT NULL,
        Name          NVARCHAR(200)  NOT NULL,
        Slug          NVARCHAR(220)  NOT NULL,
        Description   NVARCHAR(MAX)  NOT NULL,
        DegreeLevel   NVARCHAR(50)   NOT NULL,
        DurationYears INT            NOT NULL CONSTRAINT DF_Programs_DurationYears DEFAULT (4),
        IsActive      BIT            NOT NULL CONSTRAINT DF_Programs_IsActive DEFAULT (1),
        DisplayOrder  INT            NOT NULL CONSTRAINT DF_Programs_DisplayOrder DEFAULT (0),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Programs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Programs_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Programs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Programs_Code UNIQUE (Code),
        CONSTRAINT UQ_Programs_Slug UNIQUE (Slug),
        CONSTRAINT CK_Programs_DurationYears CHECK (DurationYears BETWEEN 1 AND 10)
    );
END
GO

/* --------------------------------------------------------- ContentPages ---- */
IF OBJECT_ID('dbo.ContentPages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContentPages
    (
        Id          INT            IDENTITY(1,1) NOT NULL,
        Slug        NVARCHAR(220)  NOT NULL,
        Title       NVARCHAR(200)  NOT NULL,
        Content     NVARCHAR(MAX)  NOT NULL,
        IsPublished BIT            NOT NULL CONSTRAINT DF_ContentPages_IsPublished DEFAULT (0),
        CreatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_ContentPages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_ContentPages_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ContentPages PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_ContentPages_Slug UNIQUE (Slug)
    );
END
GO

/* ----------------------------------------------------------------- Faqs ---- */
IF OBJECT_ID('dbo.Faqs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Faqs
    (
        Id        INT            IDENTITY(1,1) NOT NULL,
        Question  NVARCHAR(300)  NOT NULL,
        Answer    NVARCHAR(MAX)  NOT NULL,
        -- Comma separated keywords used by the chatbot matcher.
        Keywords  NVARCHAR(500)  NOT NULL CONSTRAINT DF_Faqs_Keywords DEFAULT (N''),
        Category  NVARCHAR(80)   NOT NULL,
        IsActive  BIT            NOT NULL CONSTRAINT DF_Faqs_IsActive DEFAULT (1),
        CreatedAt DATETIME2(3)   NOT NULL CONSTRAINT DF_Faqs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(3)   NOT NULL CONSTRAINT DF_Faqs_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Faqs PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_Faqs_IsActive ON dbo.Faqs (IsActive) INCLUDE (Category);
END
GO

/* --------------------------------------------------------- ChatMessages ---- */
IF OBJECT_ID('dbo.ChatMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ChatMessages
    (
        Id           BIGINT           IDENTITY(1,1) NOT NULL,
        SessionId    UNIQUEIDENTIFIER NOT NULL,
        UserMessage  NVARCHAR(500)    NOT NULL,
        BotReply     NVARCHAR(MAX)    NOT NULL,
        MatchedFaqId INT              NULL,
        CreatedAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_ChatMessages_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ChatMessages PRIMARY KEY CLUSTERED (Id),
        -- Deleting a FAQ keeps the conversation history, it only loses the link.
        CONSTRAINT FK_ChatMessages_Faqs FOREIGN KEY (MatchedFaqId) REFERENCES dbo.Faqs (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_ChatMessages_SessionId ON dbo.ChatMessages (SessionId, CreatedAt);
END
GO

PRINT 'BCAS_Web schema is up to date.';
GO
