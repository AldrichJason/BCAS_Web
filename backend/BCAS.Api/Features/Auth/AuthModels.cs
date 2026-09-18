namespace BCAS.Api.Features.Auth;

/// <summary>A user row joined with its role, as read for authentication.</summary>
public sealed class UserCredentialRecord
{
    public int UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public int? PrimaryDepartmentId { get; init; }

    public string? PrimaryDepartmentCode { get; init; }

    public bool IsActive { get; init; }

    public bool MustChangePassword { get; init; }

    public int FailedLoginCount { get; init; }

    public DateTime? LockedOutUntil { get; init; }
}

/// <summary>A department the user is scoped to.</summary>
public sealed class DepartmentScope
{
    public int DepartmentId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
