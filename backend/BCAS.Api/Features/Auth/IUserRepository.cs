namespace BCAS.Api.Features.Auth;

public interface IUserRepository
{
    Task<UserCredentialRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentScope>> GetDepartmentScopeAsync(int userId, CancellationToken cancellationToken = default);

    Task RecordSuccessfulLoginAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Increments the failure counter and locks the account out once it reaches
    /// <paramref name="maxFailedAttempts"/>.
    /// </summary>
    Task RecordFailedLoginAsync(int userId, int maxFailedAttempts, int lockoutMinutes, CancellationToken cancellationToken = default);
}
