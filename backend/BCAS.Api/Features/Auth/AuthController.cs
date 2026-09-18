using BCAS.Api.Common.Errors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    /// <summary>
    /// Deliberately identical for an unknown email, a wrong password and a
    /// deactivated account - the client cannot tell the three apart.
    /// </summary>
    private const string InvalidCredentialsMessage = "Incorrect email or password.";

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
}
