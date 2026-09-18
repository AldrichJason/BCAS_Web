using BCAS.Api.Models;
using BCAS.Api.Repositories.Utilities;
using Dapper;

namespace BCAS.Api.Repositories;

public sealed class TokenRevocationStore : ITokenRevocationStore
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TokenRevocationStore(ISqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task RevokeAsync(
        string jti, int userId, DateTime expiresAtUtc, CancellationToken cancellationToken = default)
    {
        // Logging out twice with the same token is not an error.
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM auth.RevokedTokens WHERE Jti = @Jti)
                INSERT INTO auth.RevokedTokens (Jti, UserId, ExpiresAt)
                VALUES (@Jti, @UserId, @ExpiresAt);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql, new { Jti = jti, UserId = userId, ExpiresAt = expiresAtUtc }, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    public async Task<AccessTokenStatus> GetStatusAsync(
        string jti, int userId, CancellationToken cancellationToken = default)
    {
        // 0 = accepted, 1 = revoked, 2 = account inactive or missing.
        const string sql = """
            SELECT CASE
                       WHEN EXISTS (SELECT 1 FROM auth.RevokedTokens WHERE Jti = @Jti) THEN 1
                       WHEN NOT EXISTS (SELECT 1 FROM auth.Users WHERE UserId = @UserId AND IsActive = 1) THEN 2
                       ELSE 0
                   END;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { Jti = jti, UserId = userId }, cancellationToken: cancellationToken);
        var code = await connection.ExecuteScalarAsync<int>(command).ConfigureAwait(false);

        return code switch
        {
            1 => AccessTokenStatus.Revoked,
            2 => AccessTokenStatus.AccountInactive,
            _ => AccessTokenStatus.Accepted,
        };
    }
}
