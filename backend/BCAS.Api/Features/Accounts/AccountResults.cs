namespace BCAS.Api.Features.Accounts;

public enum CreateUserOutcome
{
    Created,

    /// <summary>An account already uses this email address.</summary>
    DuplicateEmail,

    /// <summary>The role code is not one of the four known roles.</summary>
    UnknownRole,

    /// <summary>A department is required for this role, or was supplied when it should not be.</summary>
    InvalidDepartment,
}

public sealed record CreateUserResult(CreateUserOutcome Outcome, UserAccount? Account = null, string? Message = null)
{
    public bool Succeeded => Outcome == CreateUserOutcome.Created;
}

public enum SetActivationOutcome
{
    Updated,

    NotFound,

    /// <summary>A Super Admin cannot deactivate their own account and lock themselves out.</summary>
    CannotDeactivateSelf,

    /// <summary>Refused because it would leave no active Super Admin at all.</summary>
    LastActiveSuperAdmin,
}

public sealed record SetActivationResult(SetActivationOutcome Outcome, UserAccount? Account = null)
{
    public bool Succeeded => Outcome == SetActivationOutcome.Updated;
}
