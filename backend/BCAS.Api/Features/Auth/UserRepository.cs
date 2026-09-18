using BCAS.Api.Common.Db;
using Dapper;

namespace BCAS.Api.Features.Auth;

public sealed class UserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<UserCredentialRecord?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  u.UserId,
                    u.Email,
                    u.PasswordHash,
                    u.FirstName,
                    u.LastName,
                    r.Code               AS RoleCode,
                    r.Name               AS RoleName,
                    u.PrimaryDepartmentId,
                    d.Code               AS PrimaryDepartmentCode,
                    u.IsActive,
                    u.MustChangePassword,
                    u.FailedLoginCount,
                    u.LockedOutUntil
            FROM    auth.Users u
                    INNER JOIN auth.Roles r ON r.RoleId = u.RoleId
                    LEFT  JOIN auth.Departments d ON d.DepartmentId = u.PrimaryDepartmentId
            WHERE   u.Email = @Email;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserCredentialRecord>(command).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<DepartmentScope>> GetDepartmentScopeAsync(int userId, CancellationToken cancellationToken = default)
    {
        // The primary department is unioned in so the scope is right even if
        // provisioning did not also write an auth.UserDepartments row for it.
        const string sql = """
            SELECT   d.DepartmentId, d.Code, d.Name
            FROM     auth.Departments d
            WHERE    d.IsActive = 1
                     AND (
                           EXISTS (SELECT 1 FROM auth.UserDepartments ud
                                   WHERE ud.UserId = @UserId AND ud.DepartmentId = d.DepartmentId)
                           OR EXISTS (SELECT 1 FROM auth.Users u
                                      WHERE u.UserId = @UserId AND u.PrimaryDepartmentId = d.DepartmentId)
                         )
            ORDER BY d.Code;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<DepartmentScope>(command).ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task RecordSuccessfulLoginAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  auth.Users
            SET     LastLoginAt      = SYSUTCDATETIME(),
                    FailedLoginCount = 0,
                    LockedOutUntil   = NULL
            WHERE   UserId = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    public async Task RecordFailedLoginAsync(
        int userId, int maxFailedAttempts, int lockoutMinutes, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  auth.Users
            SET     FailedLoginCount = FailedLoginCount + 1,
                    LockedOutUntil   = CASE
                                          WHEN FailedLoginCount + 1 >= @MaxFailedAttempts
                                          THEN DATEADD(MINUTE, @LockoutMinutes, SYSUTCDATETIME())
                                          ELSE LockedOutUntil
                                       END
            WHERE   UserId = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql,
            new { UserId = userId, MaxFailedAttempts = maxFailedAttempts, LockoutMinutes = lockoutMinutes },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }
}
