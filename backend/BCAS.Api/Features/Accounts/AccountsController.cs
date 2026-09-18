using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BCAS.Api.Common.Errors;
using BCAS.Api.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Features.Accounts;

/// <summary>
/// Super Admin account administration (BW-14, BW-15). Every action on this
/// controller is Super Admin only; any other signed-in role gets HTTP 403.
/// </summary>
[ApiController]
[Route("api/admin/accounts")]
[Authorize(Roles = RoleCodes.SuperAdmin)]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AccountsController(IAccountService accounts) => _accounts = accounts;

    /// <summary>Lists every account, deactivated ones included.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserAccount>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken) =>
        Ok(await _accounts.ListAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Role and department options for the create-account form.</summary>
    [HttpGet("reference")]
    [ProducesResponseType(typeof(AccountReference), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetReferenceAsync(CancellationToken cancellationToken) =>
        Ok(await _accounts.GetReferenceAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>Provisions an account and emails its invitation link (BW-14).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserAccount), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (CurrentUserId() is not { } actorId)
        {
            return Unauthorized(new ApiError("invalid_token", "Your session is no longer valid."));
        }

        var result = await _accounts.CreateAsync(request, actorId, cancellationToken).ConfigureAwait(false);

        if (result.Succeeded)
        {
            return CreatedAtAction(nameof(ListAsync), new { id = result.Account!.UserId }, result.Account);
        }

        return result.Outcome switch
        {
            // A duplicate email is a conflict with an account that already exists,
            // not a malformed request.
            CreateUserOutcome.DuplicateEmail => Conflict(
                new ApiError("duplicate_email", result.Message ?? "An account already uses that email address.")),
            CreateUserOutcome.InvalidDepartment => BadRequest(
                new ApiError("invalid_department", result.Message ?? "The department is not valid for this role.")),
            _ => BadRequest(new ApiError("unknown_role", result.Message ?? "Choose one of the listed roles.")),
        };
    }

    /// <summary>
    /// Activates or deactivates an account (BW-15). The account is never deleted,
    /// so its authored content and activity log survive untouched.
    /// </summary>
    [HttpPut("{userId:int}/activation")]
    [ProducesResponseType(typeof(UserAccount), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SetActivationAsync(
        int userId, [FromBody] SetActivationRequest request, CancellationToken cancellationToken)
    {
        if (CurrentUserId() is not { } actorId)
        {
            return Unauthorized(new ApiError("invalid_token", "Your session is no longer valid."));
        }

        var result = await _accounts
            .SetActivationAsync(userId, request.IsActive!.Value, actorId, cancellationToken)
            .ConfigureAwait(false);

        return result.Outcome switch
        {
            SetActivationOutcome.Updated => Ok(result.Account),
            SetActivationOutcome.NotFound => NotFound(
                new ApiError("account_not_found", "That account no longer exists.")),
            SetActivationOutcome.CannotDeactivateSelf => BadRequest(
                new ApiError("cannot_deactivate_self", "You cannot deactivate your own account.")),
            _ => BadRequest(new ApiError(
                "last_super_admin",
                "This is the only active Super Admin. Activate another one before deactivating this account.")),
        };
    }

    private int? CurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
            ? userId
            : null;
    }
}
