namespace BCAS.Api.Models;

/// <summary>
/// Why a password token was issued. Both kinds are consumed the same way; they
/// differ in how long they live and what the email says.
/// </summary>
public enum PasswordTokenPurpose
{
    /// <summary>The user asked to reset a password they already had (BW-13).</summary>
    Reset,

    /// <summary>The Super Admin provisioned the account and it has no password yet (BW-14).</summary>
    Invite,
}

/// <summary>A reset token row, looked up by the hash of the raw token.</summary>
public sealed class PasswordResetTokenRecord
{
    public int TokenId { get; init; }

    public int UserId { get; init; }

    public DateTime ExpiresAt { get; init; }

    public DateTime? ConsumedAt { get; init; }

    public DateTime? InvalidatedAt { get; init; }

    public bool IsUsable(DateTime utcNow) =>
        ConsumedAt is null && InvalidatedAt is null && ExpiresAt > utcNow;
}
