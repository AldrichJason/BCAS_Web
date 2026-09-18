using BCAS.Api.Models;
using System.Security.Claims;

namespace BCAS.Api.Helpers;

public interface IJwtTokenService
{
    /// <summary>Issues a signed access token carrying user id, role and department scope.</summary>
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        UserCredentialRecord user, IReadOnlyList<DepartmentScope> departments);
}

/// <summary>Custom claim types used by this API.</summary>
public static class BcasClaimTypes
{
    /// <summary>Comma-free, repeated claim: one entry per department id in scope.</summary>
    public const string Department = "dept";

    /// <summary>"1" when the user must change their password before doing anything else.</summary>
    public const string MustChangePassword = "must_change_pwd";

    /// <summary>Standard subject claim, re-exported for readability.</summary>
    public const string Subject = ClaimTypes.NameIdentifier;
}
