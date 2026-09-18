using System.ComponentModel.DataAnnotations;

namespace BCAS.Api.Models.DTOs;

public sealed class CreateUserRequest
{
    [Required(ErrorMessage = "First name is required.")]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required.")]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required.")]
    [MaxLength(40)]
    public string RoleCode { get; set; } = string.Empty;

    /// <summary>
    /// Required for Academic Head, and must be left empty for every other role.
    /// </summary>
    public int? DepartmentId { get; set; }
}

public sealed class SetActivationRequest
{
    [Required(ErrorMessage = "Specify whether the account should be active.")]
    public bool? IsActive { get; set; }
}

/// <summary>Role and department options for the create-account form.</summary>
public sealed record AccountReference(
    IReadOnlyList<RoleOption> Roles,
    IReadOnlyList<DepartmentOption> Departments);

public sealed class RoleOption
{
    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    /// <summary>True when a department must be chosen alongside this role.</summary>
    public bool RequiresDepartment { get; init; }
}

public sealed class DepartmentOption
{
    public int DepartmentId { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
