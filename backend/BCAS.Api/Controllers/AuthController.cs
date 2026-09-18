using BCAS.Api.Helpers;
using BCAS.Api.Models;
using BCAS.Api.Models.DTOs;
using BCAS.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BCAS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Deliberately identical for an unknown email, a wrong password and a
    /// deactivated account - the client cannot tell the three apart.
    /// </summary>
    private const string InvalidCredentialsMessage = "Incorrect email or password.";

    /// <summary>
    /// Returned by forgot-password whether or not the account exists, so the
    /// endpoint cannot be used to discover which emails are registered.
    /// </summary>
    private const string ForgotPasswordMessage =
        "If that email belongs to an account, a reset link is on its way.";

    private readonly IAuthService _authService;

    public AuthController(IAuthService authService) => _authService = authService;

    /// <summary>Signs a user in and returns a signed JWT plus their basic profile.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(request, cancellationToken).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return Ok(result.Response);
        }

        var error = result.Failure switch
        {
            LoginFailureReason.LockedOut => new ApiError(
                "account_locked",
                "Too many failed sign-in attempts. Try again later or contact your administrator."),
            _ => new ApiError("invalid_credentials", InvalidCredentialsMessage),
        };

        return Unauthorized(error);
    }

    /// <summary>
    /// Returns the signed-in user, re-read from the database (BW-12). The client
    /// calls this on load to decide where to route without signing in again.
    /// </summary>
    [Authorize]
    [HttpGet("session")]
    [ProducesResponseType(typeof(SessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSessionAsync(CancellationToken cancellationToken)
    {
        if (TryGetUserId() is not { } userId)
        {
            return Unauthorized(new ApiError("invalid_token", "Your session is no longer valid."));
        }

        var user = await _authService.GetSessionAsync(userId, cancellationToken).ConfigureAwait(false);

        // The token is still signed and unexpired, but the account behind it is
        // gone or deactivated, so the session is over.
        return user is null
            ? Unauthorized(new ApiError("session_ended", "Your session is no longer valid."))
            : Ok(new SessionResponse(user));
    }

    /// <summary>
    /// Revokes the caller's access token so it cannot be replayed (BW-11).
    /// Idempotent: signing out twice is not an error.
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);

        if (TryGetUserId() is not { } userId || string.IsNullOrEmpty(jti))
        {
            return Unauthorized(new ApiError("invalid_token", "Your session is no longer valid."));
        }

        await _authService.LogoutAsync(jti, userId, TokenExpiryUtc(), cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Starts a password reset (BW-13).</summary>
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPasswordAsync(
        [FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await _authService.RequestPasswordResetAsync(request.Email, cancellationToken).ConfigureAwait(false);

        // Always the same response, whatever happened behind it.
        return Ok(new ApiMessage(ForgotPasswordMessage));
    }

    /// <summary>Completes a password reset using the token from the email link (BW-13).</summary>
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync(
        [FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await _authService.ResetPasswordAsync(request, cancellationToken).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return Ok(new ApiMessage("Your password has been changed. You can now sign in."));
        }

        var error = result.Outcome switch
        {
            PasswordResetOutcome.PasswordMismatch => new ApiError(
                "password_mismatch", result.Message ?? "The two passwords do not match."),
            PasswordResetOutcome.WeakPassword => new ApiError(
                "weak_password", result.Message ?? PasswordPolicy.Description),
            PasswordResetOutcome.AccountInactive => new ApiError(
                "account_inactive", "This account is deactivated. Contact your administrator."),
            _ => new ApiError(
                "invalid_reset_token",
                "This reset link is no longer valid. It may have expired or already been used."),
        };

        return BadRequest(error);
    }

    private int? TryGetUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
            ? userId
            : null;
    }

    /// <summary>
    /// The token's own expiry, used to decide how long the revocation row needs
    /// to be kept. Falls back to "now" so a malformed claim still revokes.
    /// </summary>
    private DateTime TokenExpiryUtc()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : DateTime.UtcNow;
    }
}
