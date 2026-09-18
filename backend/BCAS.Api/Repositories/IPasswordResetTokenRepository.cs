using BCAS.Api.Models;

namespace BCAS.Api.Repositories;

public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Invalidates any outstanding tokens for the user and stores the new one,
    /// so only the most recent reset link works.
    /// </summary>
    Task CreateAsync(
        int userId,
        string tokenHash,
        DateTime expiresAtUtc,
        string? requestedIp,
        PasswordTokenPurpose purpose,
        CancellationToken cancellationToken = default);

    Task<PasswordResetTokenRecord?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the token consumed and invalidates every other outstanding token for
    /// the same user, in one transaction. Returns false if the token was already
    /// used or invalidated - that is, if another request won the race.
    /// </summary>
    Task<bool> ConsumeAsync(int tokenId, CancellationToken cancellationToken = default);
}
