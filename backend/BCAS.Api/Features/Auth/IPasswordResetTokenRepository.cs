namespace BCAS.Api.Features.Auth;

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

public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Invalidates any outstanding tokens for the user and stores the new one,
    /// so only the most recent reset link works.
    /// </summary>
    Task CreateAsync(
        int userId, string tokenHash, DateTime expiresAtUtc, string? requestedIp, CancellationToken cancellationToken = default);

    Task<PasswordResetTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the token consumed and invalidates every other outstanding token for
    /// the same user, in one transaction. Returns false if the token was already
    /// used or invalidated - that is, if another request won the race.
    /// </summary>
    Task<bool> ConsumeAsync(int tokenId, CancellationToken cancellationToken = default);
}
