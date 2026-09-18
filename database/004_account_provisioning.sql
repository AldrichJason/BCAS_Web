/* =============================================================================
   BCAS Web Portal & CMS - Account provisioning
   BW-14: invitation links reuse the password reset token table, distinguished
          by Purpose so an invitation can have its own lifetime and wording.

   Idempotent and repeatable. Run after 003_sessions_and_password_resets.sql.
   ========================================================================== */

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF COL_LENGTH('auth.PasswordResetTokens', 'Purpose') IS NULL
BEGIN
    ALTER TABLE auth.PasswordResetTokens
        ADD Purpose VARCHAR(20) NOT NULL
            CONSTRAINT DF_PasswordResetTokens_Purpose DEFAULT ('Reset');
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PasswordResetTokens_Purpose')
BEGIN
    ALTER TABLE auth.PasswordResetTokens
        ADD CONSTRAINT CK_PasswordResetTokens_Purpose
            CHECK (Purpose IN ('Reset', 'Invite'));
END
GO

/* -----------------------------------------------------------------------------
   Reporting helper: accounts with their role and department scope.

   Deactivated accounts are included on purpose - BW-15 keeps them, and the
   admin screen needs to see them in order to reactivate them.
   -------------------------------------------------------------------------- */
CREATE OR ALTER VIEW auth.vw_UserAccounts
AS
SELECT  u.UserId,
        u.Email,
        u.FirstName,
        u.LastName,
        u.RoleId,
        r.Code            AS RoleCode,
        r.Name            AS RoleName,
        u.PrimaryDepartmentId,
        d.Code            AS PrimaryDepartmentCode,
        d.Name            AS PrimaryDepartmentName,
        u.IsActive,
        u.MustChangePassword,
        u.LastLoginAt,
        u.CreatedAt,
        u.UpdatedAt
FROM    auth.Users u
        INNER JOIN auth.Roles r ON r.RoleId = u.RoleId
        LEFT  JOIN auth.Departments d ON d.DepartmentId = u.PrimaryDepartmentId;
GO

PRINT 'BCAS account provisioning objects applied.';
GO
