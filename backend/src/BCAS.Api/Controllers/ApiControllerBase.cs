using BCAS.Application.Common;
using BCAS.Domain.Entities;
using BCAS.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

[ApiController]
[Produces("application/json")]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>True when the caller may see drafts and deactivated records.</summary>
    protected bool CanManageContent =>
        User.IsInRole(RoleNames.Administrator) || User.IsInRole(RoleNames.Editor);

    protected int CurrentUserId
    {
        get
        {
            var value = User.FindFirst(JwtClaimNames.Subject)?.Value;

            return int.TryParse(value, out var id)
                ? id
                : throw new AuthenticationFailedException("The access token does not identify a user.");
        }
    }
}
