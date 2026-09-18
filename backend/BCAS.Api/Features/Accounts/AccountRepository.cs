using BCAS.Api.Common.Db;
using BCAS.Api.Features.Auth;
using Dapper;

namespace BCAS.Api.Features.Accounts;

public sealed class AccountRepository : IAccountRepository
{
    private const string AccountColumns = """
        UserId, Email, FirstName, LastName, RoleCode, RoleName,
        PrimaryDepartmentId, PrimaryDepartmentCode, PrimaryDepartmentName,
        IsActive, MustChangePassword, LastLoginAt, CreatedAt
        """;

    private readonly ISqlConnectionFactory _connectionFactory;

    public AccountRepository(ISqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default)
    {
        // Deactivated accounts are listed too - they are kept, not deleted, and
        // the Super Admin needs to see them in order to reactivate them.
        var sql = $"""
            SELECT   {AccountColumns}
            FROM     auth.vw_UserAccounts
            ORDER BY IsActive DESC, LastName, FirstName;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection
            .QueryAsync<UserAccount>(new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }

    public async Task<UserAccount?> FindAsync(int userId, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT  {AccountColumns}
            FROM    auth.vw_UserAccounts
            WHERE   UserId = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<UserAccount>(command).ConfigureAwait(false);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql =
            "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM auth.Users WHERE Email = @Email) THEN 1 ELSE 0 END AS BIT);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }

    public async Task<int?> FindRoleIdAsync(string roleCode, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT RoleId FROM auth.Roles WHERE Code = @Code;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { Code = roleCode }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<int?>(command).ConfigureAwait(false);
    }

    public async Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CAST(CASE WHEN EXISTS (
                SELECT 1 FROM auth.Departments WHERE DepartmentId = @DepartmentId AND IsActive = 1
            ) THEN 1 ELSE 0 END AS BIT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { DepartmentId = departmentId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }

    public async Task<int> CreateAsync(
        CreateUserRequest request,
        int roleId,
        string passwordHash,
        int createdByUserId,
        CancellationToken cancellationToken = default)
    {
        // The account starts active with MustChangePassword set: the invitation
        // link is what turns the unusable placeholder hash into a real password.
        const string sql = """
            SET NOCOUNT ON;
            BEGIN TRANSACTION;

            INSERT INTO auth.Users
                (Email, PasswordHash, FirstName, LastName, RoleId, PrimaryDepartmentId,
                 IsActive, MustChangePassword, CreatedBy)
            VALUES
                (@Email, @PasswordHash, @FirstName, @LastName, @RoleId, @DepartmentId,
                 1, 1, @CreatedBy);

            DECLARE @NewUserId INT = CAST(SCOPE_IDENTITY() AS INT);

            IF @DepartmentId IS NOT NULL
                INSERT INTO auth.UserDepartments (UserId, DepartmentId, GrantedBy)
                VALUES (@NewUserId, @DepartmentId, @CreatedBy);

            COMMIT TRANSACTION;

            SELECT @NewUserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql,
            new
            {
                request.Email,
                PasswordHash = passwordHash,
                request.FirstName,
                request.LastName,
                RoleId = roleId,
                request.DepartmentId,
                CreatedBy = createdByUserId,
            },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);
    }

    public async Task SetActivationAsync(
        int userId, bool isActive, int changedByUserId, CancellationToken cancellationToken = default)
    {
        // A flag flip and nothing else: no row is removed here or anywhere else,
        // so authored content keeps its author and the activity log is untouched.
        const string sql = """
            UPDATE  auth.Users
            SET     IsActive         = @IsActive,
                    UpdatedBy        = @ChangedBy,
                    UpdatedAt        = SYSUTCDATETIME(),
                    -- Reactivating restores access directly: any lockout left over
                    -- from before is cleared so no re-provisioning is needed.
                    FailedLoginCount = CASE WHEN @IsActive = 1 THEN 0 ELSE FailedLoginCount END,
                    LockedOutUntil   = CASE WHEN @IsActive = 1 THEN NULL ELSE LockedOutUntil END
            WHERE   UserId = @UserId;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql,
            new { UserId = userId, IsActive = isActive, ChangedBy = changedByUserId },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    public async Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  COUNT(*)
            FROM    auth.Users u
                    INNER JOIN auth.Roles r ON r.RoleId = u.RoleId
            WHERE   r.Code = @SuperAdmin
                    AND u.IsActive = 1;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql, new { SuperAdmin = RoleCodes.SuperAdmin }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<RoleOption>> ListRolesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Code, Name, Description FROM auth.Roles ORDER BY RoleId;";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection
            .QueryAsync<RoleOption>(new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        // RequiresDepartment is a rule about the role, not a stored column.
        return rows
            .Select(r => new RoleOption
            {
                Code = r.Code,
                Name = r.Name,
                Description = r.Description,
                RequiresDepartment = RoleCodes.RequiresDepartment(r.Code),
            })
            .ToList();
    }

    public async Task<IReadOnlyList<DepartmentOption>> ListDepartmentsAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT   DepartmentId, Code, Name
            FROM     auth.Departments
            WHERE    IsActive = 1
            ORDER BY Code;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var rows = await connection
            .QueryAsync<DepartmentOption>(new CommandDefinition(sql, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        return rows.AsList();
    }
}
