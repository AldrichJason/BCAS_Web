using BCAS.Api.Common.Db;
using Dapper;

namespace BCAS.Api.Features.Auth;

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

    public async Task<bool> IsRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM auth.RevokedTokens WHERE Jti = @Jti) THEN 1 ELSE 0 END AS BIT);";

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { Jti = jti }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }
}
