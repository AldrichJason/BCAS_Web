using BCAS.Api.Models;

namespace BCAS.Api.Repositories;

public interface IUserRepository
{
    Task<UserCredentialRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentScope>> GetDepartmentScopeAsync(int userId, CancellationToken cancellationToken = default);

    Task<UserCredentialRecord?> FindByIdAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the stored hash, clears MustChangePassword and releases any
    /// lockout, so a reset also unblocks an account locked by failed attempts.
    /// </summary>
    Task UpdatePasswordAsync(int userId, string passwordHash, CancellationToken cancellationToken = default);

    Task RecordSuccessfulLoginAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the failure counter and locks the account out once it reaches
    /// <paramref name="maxFailedAttempts"/>.
    /// </summary>
    Task RecordFailedLoginAsync(int userId, int maxFailedAttempts, int lockoutMinutes, CancellationToken cancellationToken = default);
}
