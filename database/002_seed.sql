/* =============================================================================
   BCAS Web Portal & CMS - Seed data
   BW-9: four departments, the role set, content statuses and a Super Admin.

   Idempotent: safe to re-run against an already-seeded database.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* --- Content statuses ----------------------------------------------------- */
MERGE content.ContentStatuses AS target
USING (VALUES
    (1, 'Draft',     N'Draft'),
    (2, 'Scheduled', N'Scheduled'),
    (3, 'Published', N'Published'),
    (4, 'Archived',  N'Archived')
) AS source (StatusId, Code, Name)
ON target.StatusId = source.StatusId
WHEN MATCHED THEN UPDATE SET Code = source.Code, Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (StatusId, Code, Name) VALUES (source.StatusId, source.Code, source.Name);
GO

/* --- Roles ---------------------------------------------------------------- */
MERGE auth.Roles AS target
USING (VALUES
    ('SUPER_ADMIN',   N'Super Admin',           N'Full access to every module, account provisioning and audit.'),
    ('ACADEMIC_HEAD', N'Academic Head',         N'Manages content scoped to their own department.'),
    ('REGISTRAR',     N'Admin Office/Registrar', N'Manages admissions, services, policies and the inquiry inbox.'),
    ('VP_OPERATIONS', N'VP of Operations',      N'School-wide oversight, content approval and reporting.')
) AS source (Code, Name, Description)
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET Name = source.Name, Description = source.Description
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name, Description) VALUES (source.Code, source.Name, source.Description);
GO

/* --- Departments (the four academic departments) -------------------------- */
MERGE auth.Departments AS target
USING (VALUES
    ('CCS',  N'College of Computer Studies'),
    ('CBA',  N'College of Business and Accountancy'),
    ('CEDU', N'College of Education'),
    ('CHAS', N'College of Hospitality and Arts Studies')
) AS source (Code, Name)
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET Name = source.Name
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Code, Name) VALUES (source.Code, source.Name);
GO

/* --- Initial Super Admin --------------------------------------------------
   Email    : superadmin@bcas.edu.ph
   Password : ChangeMe!2026

   The hash below is PBKDF2-HMAC-SHA256, 210,000 iterations, 16-byte salt,
   32-byte key, stored as: pbkdf2-sha256$<iterations>$<b64 salt>$<b64 hash>.
   MustChangePassword is set, so this credential cannot survive first login.
   Rotate it before any deployment that is reachable outside localhost.
   ------------------------------------------------------------------------- */
DECLARE @SuperAdminRoleId INT = (SELECT RoleId FROM auth.Roles WHERE Code = 'SUPER_ADMIN');

IF NOT EXISTS (SELECT 1 FROM auth.Users WHERE Email = N'superadmin@bcas.edu.ph')
BEGIN
    INSERT INTO auth.Users
        (Email, PasswordHash, FirstName, LastName, RoleId, PrimaryDepartmentId, IsActive, MustChangePassword)
    VALUES
        (N'superadmin@bcas.edu.ph',
         'pbkdf2-sha256$210000$pqAumAyu9iTMhfmNXO8ISg==$n+S8cEZu0oCp1qyUyaqSlCyKb2phhO7fCZ6pgWbaab0=',
         N'BCAS', N'Super Admin', @SuperAdminRoleId, NULL, 1, 1);
END
GO

PRINT 'BCAS seed data applied.';
GO
