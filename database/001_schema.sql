/* =============================================================================
   BCAS Web Portal & CMS - Database Schema
   Target: SQL Server 2019+
   BW-9: Design and implement the database schema

   Idempotent and repeatable from an empty database. Run 001_schema.sql first,
   then 002_seed.sql. See database/README.md for run instructions.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* -----------------------------------------------------------------------------
   Schemas
   -------------------------------------------------------------------------- */
IF SCHEMA_ID('auth')    IS NULL EXEC('CREATE SCHEMA auth');
IF SCHEMA_ID('content') IS NULL EXEC('CREATE SCHEMA content');
IF SCHEMA_ID('ops')     IS NULL EXEC('CREATE SCHEMA ops');
GO

/* -----------------------------------------------------------------------------
   Reference data: departments, roles, content status
   -------------------------------------------------------------------------- */
IF OBJECT_ID('auth.Departments', 'U') IS NULL
BEGIN
    CREATE TABLE auth.Departments
    (
        DepartmentId  INT            IDENTITY(1,1) NOT NULL,
        Code          VARCHAR(20)    NOT NULL,
        Name          NVARCHAR(150)  NOT NULL,
        IsActive      BIT            NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (DepartmentId),
        CONSTRAINT UQ_Departments_Code UNIQUE (Code)
    );
END
GO

IF OBJECT_ID('auth.Roles', 'U') IS NULL
BEGIN
    CREATE TABLE auth.Roles
    (
        RoleId        INT            IDENTITY(1,1) NOT NULL,
        Code          VARCHAR(40)    NOT NULL,   -- stable identifier used in JWT claims
        Name          NVARCHAR(100)  NOT NULL,
        Description   NVARCHAR(400)  NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
        CONSTRAINT UQ_Roles_Code UNIQUE (Code)
    );
END
GO

-- Draft / Scheduled / Published / Archived, shared by every content table.
IF OBJECT_ID('content.ContentStatuses', 'U') IS NULL
BEGIN
    CREATE TABLE content.ContentStatuses
    (
        StatusId   TINYINT       NOT NULL,
        Code       VARCHAR(20)   NOT NULL,
        Name       NVARCHAR(50)  NOT NULL,
        CONSTRAINT PK_ContentStatuses PRIMARY KEY CLUSTERED (StatusId),
        CONSTRAINT UQ_ContentStatuses_Code UNIQUE (Code)
    );
END
GO

/* -----------------------------------------------------------------------------
   Users and department scope
   -------------------------------------------------------------------------- */
IF OBJECT_ID('auth.Users', 'U') IS NULL
BEGIN
    CREATE TABLE auth.Users
    (
        UserId              INT             IDENTITY(1,1) NOT NULL,
        Email               NVARCHAR(256)   NOT NULL,
        -- PBKDF2-HMAC-SHA256, encoded as: pbkdf2-sha256$<iterations>$<b64 salt>$<b64 hash>
        PasswordHash        VARCHAR(512)    NOT NULL,
        FirstName           NVARCHAR(100)   NOT NULL,
        LastName            NVARCHAR(100)   NOT NULL,
        RoleId              INT             NOT NULL,
        -- Primary department. NULL for school-wide roles (Super Admin, VP of Operations).
        PrimaryDepartmentId INT             NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        MustChangePassword  BIT             NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT (0),
        FailedLoginCount    INT             NOT NULL CONSTRAINT DF_Users_FailedLoginCount DEFAULT (0),
        LockedOutUntil      DATETIME2(3)    NULL,
        LastLoginAt         DATETIME2(3)    NULL,
        CreatedBy           INT             NULL,
        CreatedAt           DATETIME2(3)    NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy           INT             NULL,
        UpdatedAt           DATETIME2(3)    NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES auth.Roles (RoleId),
        CONSTRAINT FK_Users_Departments FOREIGN KEY (PrimaryDepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Users_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    -- Email is the login identifier: unique, case-insensitive via the default collation.
    CREATE UNIQUE INDEX UX_Users_Email ON auth.Users (Email);
    CREATE INDEX IX_Users_RoleId ON auth.Users (RoleId);
    CREATE INDEX IX_Users_PrimaryDepartmentId ON auth.Users (PrimaryDepartmentId) WHERE PrimaryDepartmentId IS NOT NULL;
END
GO

-- A user may be scoped to more than one department (e.g. an Academic Head
-- covering two programs). The primary department is also listed here.
IF OBJECT_ID('auth.UserDepartments', 'U') IS NULL
BEGIN
    CREATE TABLE auth.UserDepartments
    (
        UserId       INT          NOT NULL,
        DepartmentId INT          NOT NULL,
        GrantedAt    DATETIME2(3) NOT NULL CONSTRAINT DF_UserDepartments_GrantedAt DEFAULT (SYSUTCDATETIME()),
        GrantedBy    INT          NULL,
        CONSTRAINT PK_UserDepartments PRIMARY KEY CLUSTERED (UserId, DepartmentId),
        -- No cascade: accounts are soft-deactivated (Users.IsActive), never hard-deleted,
        -- and a second FK to auth.Users on GrantedBy would make a cascade path ambiguous.
        CONSTRAINT FK_UserDepartments_Users FOREIGN KEY (UserId) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_UserDepartments_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_UserDepartments_GrantedBy FOREIGN KEY (GrantedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_UserDepartments_DepartmentId ON auth.UserDepartments (DepartmentId);
END
GO

/* -----------------------------------------------------------------------------
   Images (shared media library, referenced by content tables)
   -------------------------------------------------------------------------- */
IF OBJECT_ID('content.Images', 'U') IS NULL
BEGIN
    CREATE TABLE content.Images
    (
        ImageId      INT             IDENTITY(1,1) NOT NULL,
        FileName     NVARCHAR(260)   NOT NULL,
        StoragePath  NVARCHAR(500)   NOT NULL,
        ContentType  VARCHAR(100)    NOT NULL,
        ByteSize     BIGINT          NOT NULL,
        Width        INT             NULL,
        Height       INT             NULL,
        AltText      NVARCHAR(300)   NULL,
        DepartmentId INT             NULL,   -- NULL = school-wide media
        CreatedBy    INT             NOT NULL,
        CreatedAt    DATETIME2(3)    NOT NULL CONSTRAINT DF_Images_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy    INT             NULL,
        UpdatedAt    DATETIME2(3)    NULL,
        CONSTRAINT PK_Images PRIMARY KEY CLUSTERED (ImageId),
        CONSTRAINT FK_Images_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Images_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Images_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT CK_Images_ByteSize CHECK (ByteSize > 0)
    );

    CREATE INDEX IX_Images_DepartmentId ON content.Images (DepartmentId);
END
GO

/* -----------------------------------------------------------------------------
   Content tables

   Every content table carries the same contract:
     - DepartmentId  NULL = school-wide, otherwise scoped to one department
     - StatusId      Draft / Scheduled / Published / Archived
     - PublishAt     when a Scheduled item goes live (NULL for Draft)
     - audit columns CreatedBy / CreatedAt / UpdatedBy / UpdatedAt
   -------------------------------------------------------------------------- */
IF OBJECT_ID('content.News', 'U') IS NULL
BEGIN
    CREATE TABLE content.News
    (
        NewsId       INT            IDENTITY(1,1) NOT NULL,
        Title        NVARCHAR(250)  NOT NULL,
        Slug         VARCHAR(280)   NOT NULL,
        Summary      NVARCHAR(600)  NULL,
        Body         NVARCHAR(MAX)  NOT NULL,
        CoverImageId INT            NULL,
        DepartmentId INT            NULL,
        StatusId     TINYINT        NOT NULL,
        PublishAt    DATETIME2(3)   NULL,
        CreatedBy    INT            NOT NULL,
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_News_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy    INT            NULL,
        UpdatedAt    DATETIME2(3)   NULL,
        CONSTRAINT PK_News PRIMARY KEY CLUSTERED (NewsId),
        CONSTRAINT UQ_News_Slug UNIQUE (Slug),
        CONSTRAINT FK_News_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_News_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_News_Images FOREIGN KEY (CoverImageId) REFERENCES content.Images (ImageId),
        CONSTRAINT FK_News_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_News_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_News_Status_Publish ON content.News (StatusId, PublishAt DESC) INCLUDE (Title, DepartmentId);
    CREATE INDEX IX_News_DepartmentId ON content.News (DepartmentId);
END
GO

IF OBJECT_ID('content.Announcements', 'U') IS NULL
BEGIN
    CREATE TABLE content.Announcements
    (
        AnnouncementId INT            IDENTITY(1,1) NOT NULL,
        Title          NVARCHAR(250)  NOT NULL,
        Slug           VARCHAR(280)   NOT NULL,
        Body           NVARCHAR(MAX)  NOT NULL,
        IsPinned       BIT            NOT NULL CONSTRAINT DF_Announcements_IsPinned DEFAULT (0),
        ExpiresAt      DATETIME2(3)   NULL,
        DepartmentId   INT            NULL,
        StatusId       TINYINT        NOT NULL,
        PublishAt      DATETIME2(3)   NULL,
        CreatedBy      INT            NOT NULL,
        CreatedAt      DATETIME2(3)   NOT NULL CONSTRAINT DF_Announcements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy      INT            NULL,
        UpdatedAt      DATETIME2(3)   NULL,
        CONSTRAINT PK_Announcements PRIMARY KEY CLUSTERED (AnnouncementId),
        CONSTRAINT UQ_Announcements_Slug UNIQUE (Slug),
        CONSTRAINT FK_Announcements_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Announcements_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_Announcements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Announcements_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_Announcements_Status_Publish ON content.Announcements (StatusId, PublishAt DESC);
    CREATE INDEX IX_Announcements_DepartmentId ON content.Announcements (DepartmentId);
END
GO

IF OBJECT_ID('content.Events', 'U') IS NULL
BEGIN
    CREATE TABLE content.Events
    (
        EventId      INT            IDENTITY(1,1) NOT NULL,
        Title        NVARCHAR(250)  NOT NULL,
        Slug         VARCHAR(280)   NOT NULL,
        Description  NVARCHAR(MAX)  NOT NULL,
        Location     NVARCHAR(250)  NULL,
        StartsAt     DATETIME2(3)   NOT NULL,
        EndsAt       DATETIME2(3)   NULL,
        CoverImageId INT            NULL,
        DepartmentId INT            NULL,
        StatusId     TINYINT        NOT NULL,
        PublishAt    DATETIME2(3)   NULL,
        CreatedBy    INT            NOT NULL,
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Events_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy    INT            NULL,
        UpdatedAt    DATETIME2(3)   NULL,
        CONSTRAINT PK_Events PRIMARY KEY CLUSTERED (EventId),
        CONSTRAINT UQ_Events_Slug UNIQUE (Slug),
        CONSTRAINT FK_Events_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Events_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_Events_Images FOREIGN KEY (CoverImageId) REFERENCES content.Images (ImageId),
        CONSTRAINT FK_Events_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Events_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT CK_Events_Dates CHECK (EndsAt IS NULL OR EndsAt >= StartsAt)
    );

    CREATE INDEX IX_Events_StartsAt ON content.Events (StartsAt DESC) INCLUDE (StatusId, DepartmentId);
    CREATE INDEX IX_Events_DepartmentId ON content.Events (DepartmentId);
END
GO

IF OBJECT_ID('content.AcademicPrograms', 'U') IS NULL
BEGIN
    CREATE TABLE content.AcademicPrograms
    (
        ProgramId    INT            IDENTITY(1,1) NOT NULL,
        Name         NVARCHAR(250)  NOT NULL,
        Slug         VARCHAR(280)   NOT NULL,
        Abbreviation NVARCHAR(30)   NULL,
        Overview     NVARCHAR(MAX)  NOT NULL,
        Curriculum   NVARCHAR(MAX)  NULL,
        Careers      NVARCHAR(MAX)  NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_AcademicPrograms_DisplayOrder DEFAULT (0),
        DepartmentId INT            NULL,
        StatusId     TINYINT        NOT NULL,
        PublishAt    DATETIME2(3)   NULL,
        CreatedBy    INT            NOT NULL,
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_AcademicPrograms_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy    INT            NULL,
        UpdatedAt    DATETIME2(3)   NULL,
        CONSTRAINT PK_AcademicPrograms PRIMARY KEY CLUSTERED (ProgramId),
        CONSTRAINT UQ_AcademicPrograms_Slug UNIQUE (Slug),
        CONSTRAINT FK_AcademicPrograms_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_AcademicPrograms_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_AcademicPrograms_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_AcademicPrograms_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_AcademicPrograms_DepartmentId ON content.AcademicPrograms (DepartmentId);
END
GO

IF OBJECT_ID('content.AdmissionRequirements', 'U') IS NULL
BEGIN
    CREATE TABLE content.AdmissionRequirements
    (
        RequirementId INT            IDENTITY(1,1) NOT NULL,
        Title         NVARCHAR(250)  NOT NULL,
        Slug          VARCHAR(280)   NOT NULL,
        -- e.g. Freshman, Transferee, Returnee, Graduate
        ApplicantType NVARCHAR(80)   NOT NULL,
        Body          NVARCHAR(MAX)  NOT NULL,
        DisplayOrder  INT            NOT NULL CONSTRAINT DF_AdmissionRequirements_DisplayOrder DEFAULT (0),
        DepartmentId  INT            NULL,
        StatusId      TINYINT        NOT NULL,
        PublishAt     DATETIME2(3)   NULL,
        CreatedBy     INT            NOT NULL,
        CreatedAt     DATETIME2(3)   NOT NULL CONSTRAINT DF_AdmissionRequirements_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy     INT            NULL,
        UpdatedAt     DATETIME2(3)   NULL,
        CONSTRAINT PK_AdmissionRequirements PRIMARY KEY CLUSTERED (RequirementId),
        CONSTRAINT UQ_AdmissionRequirements_Slug UNIQUE (Slug),
        CONSTRAINT FK_AdmissionRequirements_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_AdmissionRequirements_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_AdmissionRequirements_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_AdmissionRequirements_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_AdmissionRequirements_DepartmentId ON content.AdmissionRequirements (DepartmentId);
END
GO

IF OBJECT_ID('content.ServicesPolicies', 'U') IS NULL
BEGIN
    CREATE TABLE content.ServicesPolicies
    (
        ServicePolicyId INT            IDENTITY(1,1) NOT NULL,
        Title           NVARCHAR(250)  NOT NULL,
        Slug            VARCHAR(280)   NOT NULL,
        -- 'Service' or 'Policy'
        Kind            VARCHAR(20)    NOT NULL,
        Body            NVARCHAR(MAX)  NOT NULL,
        EffectiveDate   DATE           NULL,
        DisplayOrder    INT            NOT NULL CONSTRAINT DF_ServicesPolicies_DisplayOrder DEFAULT (0),
        DepartmentId    INT            NULL,
        StatusId        TINYINT        NOT NULL,
        PublishAt       DATETIME2(3)   NULL,
        CreatedBy       INT            NOT NULL,
        CreatedAt       DATETIME2(3)   NOT NULL CONSTRAINT DF_ServicesPolicies_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy       INT            NULL,
        UpdatedAt       DATETIME2(3)   NULL,
        CONSTRAINT PK_ServicesPolicies PRIMARY KEY CLUSTERED (ServicePolicyId),
        CONSTRAINT UQ_ServicesPolicies_Slug UNIQUE (Slug),
        CONSTRAINT CK_ServicesPolicies_Kind CHECK (Kind IN ('Service', 'Policy')),
        CONSTRAINT FK_ServicesPolicies_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_ServicesPolicies_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_ServicesPolicies_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_ServicesPolicies_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_ServicesPolicies_DepartmentId ON content.ServicesPolicies (DepartmentId);
END
GO

IF OBJECT_ID('content.Faqs', 'U') IS NULL
BEGIN
    CREATE TABLE content.Faqs
    (
        FaqId        INT            IDENTITY(1,1) NOT NULL,
        Question     NVARCHAR(500)  NOT NULL,
        Answer       NVARCHAR(MAX)  NOT NULL,
        Category     NVARCHAR(100)  NULL,
        DisplayOrder INT            NOT NULL CONSTRAINT DF_Faqs_DisplayOrder DEFAULT (0),
        DepartmentId INT            NULL,
        StatusId     TINYINT        NOT NULL,
        PublishAt    DATETIME2(3)   NULL,
        CreatedBy    INT            NOT NULL,
        CreatedAt    DATETIME2(3)   NOT NULL CONSTRAINT DF_Faqs_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedBy    INT            NULL,
        UpdatedAt    DATETIME2(3)   NULL,
        CONSTRAINT PK_Faqs PRIMARY KEY CLUSTERED (FaqId),
        CONSTRAINT FK_Faqs_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Faqs_Statuses FOREIGN KEY (StatusId) REFERENCES content.ContentStatuses (StatusId),
        CONSTRAINT FK_Faqs_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Faqs_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_Faqs_Category ON content.Faqs (Category, DisplayOrder);
    CREATE INDEX IX_Faqs_DepartmentId ON content.Faqs (DepartmentId);
END
GO

/* -----------------------------------------------------------------------------
   Inquiries: one inbox for both Contact Us messages and visitor FAQ questions
   -------------------------------------------------------------------------- */
IF OBJECT_ID('ops.Inquiries', 'U') IS NULL
BEGIN
    CREATE TABLE ops.Inquiries
    (
        InquiryId     INT             IDENTITY(1,1) NOT NULL,
        -- 'Contact' = Contact Us form, 'Faq' = question asked from the FAQ page
        Source        VARCHAR(20)     NOT NULL,
        FullName      NVARCHAR(200)   NOT NULL,
        Email         NVARCHAR(256)   NOT NULL,
        ContactNumber NVARCHAR(40)    NULL,
        Subject       NVARCHAR(250)   NULL,
        Message       NVARCHAR(MAX)   NOT NULL,
        -- 'New' -> 'InProgress' -> 'Resolved' / 'Spam'
        Status        VARCHAR(20)     NOT NULL CONSTRAINT DF_Inquiries_Status DEFAULT ('New'),
        DepartmentId  INT             NULL,   -- routed department, NULL = unrouted / general
        AssignedTo    INT             NULL,
        ResponseNote  NVARCHAR(MAX)   NULL,
        RespondedBy   INT             NULL,
        RespondedAt   DATETIME2(3)    NULL,
        SubmittedAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_Inquiries_SubmittedAt DEFAULT (SYSUTCDATETIME()),
        SubmitterIp   VARCHAR(45)     NULL,
        CONSTRAINT PK_Inquiries PRIMARY KEY CLUSTERED (InquiryId),
        CONSTRAINT CK_Inquiries_Source CHECK (Source IN ('Contact', 'Faq')),
        CONSTRAINT CK_Inquiries_Status CHECK (Status IN ('New', 'InProgress', 'Resolved', 'Spam')),
        CONSTRAINT FK_Inquiries_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT FK_Inquiries_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES auth.Users (UserId),
        CONSTRAINT FK_Inquiries_RespondedBy FOREIGN KEY (RespondedBy) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_Inquiries_Status_Submitted ON ops.Inquiries (Status, SubmittedAt DESC);
    CREATE INDEX IX_Inquiries_DepartmentId ON ops.Inquiries (DepartmentId);
END
GO

/* -----------------------------------------------------------------------------
   Activity log: who did what, to which record. Append-only.
   -------------------------------------------------------------------------- */
IF OBJECT_ID('ops.ActivityLog', 'U') IS NULL
BEGIN
    CREATE TABLE ops.ActivityLog
    (
        ActivityId   BIGINT          IDENTITY(1,1) NOT NULL,
        UserId       INT             NULL,   -- NULL for anonymous/system actions
        -- e.g. LoginSucceeded, LoginFailed, ContentPublished, AccountDeactivated
        Action       VARCHAR(60)     NOT NULL,
        EntityType   VARCHAR(60)     NULL,   -- e.g. 'News', 'User'
        EntityId     VARCHAR(60)     NULL,
        Detail       NVARCHAR(1000)  NULL,
        IpAddress    VARCHAR(45)     NULL,
        UserAgent    NVARCHAR(400)   NULL,
        OccurredAt   DATETIME2(3)    NOT NULL CONSTRAINT DF_ActivityLog_OccurredAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_ActivityLog PRIMARY KEY CLUSTERED (ActivityId),
        CONSTRAINT FK_ActivityLog_Users FOREIGN KEY (UserId) REFERENCES auth.Users (UserId)
    );

    CREATE INDEX IX_ActivityLog_OccurredAt ON ops.ActivityLog (OccurredAt DESC);
    CREATE INDEX IX_ActivityLog_User_OccurredAt ON ops.ActivityLog (UserId, OccurredAt DESC);
    CREATE INDEX IX_ActivityLog_Action ON ops.ActivityLog (Action, OccurredAt DESC);
END
GO

/* -----------------------------------------------------------------------------
   Search analytics: what visitors search for on the public portal
   -------------------------------------------------------------------------- */
IF OBJECT_ID('ops.SearchAnalytics', 'U') IS NULL
BEGIN
    CREATE TABLE ops.SearchAnalytics
    (
        SearchId     BIGINT         IDENTITY(1,1) NOT NULL,
        Term         NVARCHAR(300)  NOT NULL,
        NormalizedTerm AS LOWER(LTRIM(RTRIM(Term))) PERSISTED,
        ResultCount  INT            NOT NULL CONSTRAINT DF_SearchAnalytics_ResultCount DEFAULT (0),
        DepartmentId INT            NULL,   -- department filter used, if any
        SessionKey   VARCHAR(64)    NULL,   -- opaque visitor key, not a user id
        SearchedAt   DATETIME2(3)   NOT NULL CONSTRAINT DF_SearchAnalytics_SearchedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SearchAnalytics PRIMARY KEY CLUSTERED (SearchId),
        CONSTRAINT FK_SearchAnalytics_Departments FOREIGN KEY (DepartmentId) REFERENCES auth.Departments (DepartmentId),
        CONSTRAINT CK_SearchAnalytics_ResultCount CHECK (ResultCount >= 0)
    );

    CREATE INDEX IX_SearchAnalytics_Term ON ops.SearchAnalytics (NormalizedTerm, SearchedAt DESC);
    CREATE INDEX IX_SearchAnalytics_SearchedAt ON ops.SearchAnalytics (SearchedAt DESC);
END
GO

PRINT 'BCAS schema applied.';
GO
