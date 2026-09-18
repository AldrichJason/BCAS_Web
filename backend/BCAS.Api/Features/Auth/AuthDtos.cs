using System.ComponentModel.DataAnnotations;

namespace BCAS.Api.Features.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}

/// <param name="AccessToken">Signed JWT to send as <c>Authorization: Bearer</c>.</param>
/// <param name="ExpiresAt">Token expiry, UTC.</param>
/// <param name="User">Basic profile for the signed-in user.</param>
public sealed record LoginResponse(string AccessToken, DateTime ExpiresAt, AuthenticatedUser User);

public sealed record AuthenticatedUser(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string RoleCode,
    string RoleName,
    bool MustChangePassword,
    IReadOnlyList<DepartmentScope> Departments);
