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

/// <summary>Response of <c>GET /api/auth/session</c> (BW-12).</summary>
public sealed record SessionResponse(AuthenticatedUser User);

public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
}

public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "The reset link is missing its token.")]
    [MaxLength(200)]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [MaxLength(PasswordPolicy.MaximumLength)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm your new password.")]
    [MaxLength(PasswordPolicy.MaximumLength)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
