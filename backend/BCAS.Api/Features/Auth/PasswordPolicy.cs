namespace BCAS.Api.Features.Auth;

/// <summary>
/// The single definition of what counts as an acceptable password. The frontend
/// mirrors these rules in <c>src/features/auth/passwordPolicy.ts</c> for inline
/// feedback; this copy is the one that decides.
/// </summary>
public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    public const int MaximumLength = 200;

    public const string Description =
        "Password must be at least 10 characters and include an uppercase letter, " +
        "a lowercase letter and a number.";

    /// <summary>Returns null when the password is acceptable, otherwise the reason.</summary>
    public static string? Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
        {
            return Description;
        }

        if (password.Length > MaximumLength)
        {
            return $"Password must be {MaximumLength} characters or fewer.";
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c))
            {
                hasUpper = true;
            }
            else if (char.IsLower(c))
            {
                hasLower = true;
            }
            else if (char.IsDigit(c))
            {
                hasDigit = true;
            }
        }

        return hasUpper && hasLower && hasDigit ? null : Description;
    }
}
