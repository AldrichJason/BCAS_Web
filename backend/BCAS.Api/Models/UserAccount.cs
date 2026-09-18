namespace BCAS.Api.Models;

/// <summary>One account as shown on the Super Admin's user list.</summary>
public sealed class UserAccount
{
    public int UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string RoleCode { get; init; } = string.Empty;

    public string RoleName { get; init; } = string.Empty;

    public int? PrimaryDepartmentId { get; init; }

    public string? PrimaryDepartmentCode { get; init; }

    public string? PrimaryDepartmentName { get; init; }

    public bool IsActive { get; init; }

    public bool MustChangePassword { get; init; }

    public DateTime? LastLoginAt { get; init; }

    public DateTime CreatedAt { get; init; }
}
