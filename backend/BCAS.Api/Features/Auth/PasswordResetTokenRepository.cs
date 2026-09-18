using BCAS.Api.Common.Db;
using Dapper;

namespace BCAS.Api.Features.Auth;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PasswordResetTokenRepository(ISqlConnectionFactory connectionFactory) =>
        _connectionFactory = connectionFactory;

    public async Task CreateAsync(
        int userId, string tokenHash, DateTime expiresAtUtc, string? requestedIp, CancellationToken cancellationToken = default)
    {
        // Requesting a new link retires the previous ones straight away.
        const string sql = """
            BEGIN TRANSACTION;

            UPDATE  auth.PasswordResetTokens
            SET     InvalidatedAt = SYSUTCDATETIME()
            WHERE   UserId = @UserId
                    AND ConsumedAt IS NULL
                    AND InvalidatedAt IS NULL;

            INSERT INTO auth.PasswordResetTokens (UserId, TokenHash, ExpiresAt, RequestedIp)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @RequestedIp);

            COMMIT TRANSACTION;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(
            sql,
            new { UserId = userId, TokenHash = tokenHash, ExpiresAt = expiresAtUtc, RequestedIp = requestedIp },
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command).ConfigureAwait(false);
    }

    public async Task<PasswordResetTokenRecord?> FindByHashAsync(
        string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  TokenId, UserId, ExpiresAt, ConsumedAt, InvalidatedAt
            FROM    auth.PasswordResetTokens
            WHERE   TokenHash = @TokenHash;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { TokenHash = tokenHash }, cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<PasswordResetTokenRecord>(command).ConfigureAwait(false);
    }

    public async Task<bool> ConsumeAsync(int tokenId, CancellationToken cancellationToken = default)
    {
        // The WHERE clause is what makes the token single-use: if a concurrent
        // request consumed it first, no row is updated and this returns false.
        const string sql = """
            SET NOCOUNT ON;
            BEGIN TRANSACTION;

            DECLARE @Updated TABLE (UserId INT);

            UPDATE  auth.PasswordResetTokens
            SET     ConsumedAt = SYSUTCDATETIME()
            OUTPUT  inserted.UserId INTO @Updated
            WHERE   TokenId = @TokenId
                    AND ConsumedAt IS NULL
                    AND InvalidatedAt IS NULL
                    AND ExpiresAt > SYSUTCDATETIME();

            -- A successful reset retires every other outstanding token.
            UPDATE  t
            SET     InvalidatedAt = SYSUTCDATETIME()
            FROM    auth.PasswordResetTokens t
                    INNER JOIN @Updated u ON u.UserId = t.UserId
            WHERE   t.TokenId <> @TokenId
                    AND t.ConsumedAt IS NULL
                    AND t.InvalidatedAt IS NULL;

            COMMIT TRANSACTION;

            SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM @Updated) THEN 1 ELSE 0 END AS BIT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var command = new CommandDefinition(sql, new { TokenId = tokenId }, cancellationToken: cancellationToken);
        return await connection.ExecuteScalarAsync<bool>(command).ConfigureAwait(false);
    }
}
