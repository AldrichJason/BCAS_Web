using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RefreshTokenRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.RefreshTokens (UserId, TokenHash, ExpiresAt, CreatedAt)
            VALUES (@UserId, @TokenHash, @ExpiresAt, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        token.Id = await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            token,
            cancellationToken: cancellationToken));
    }

    public async Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  Id, UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt
            FROM    dbo.RefreshTokens
            WHERE   TokenHash = @TokenHash;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(new CommandDefinition(
            sql,
            new { TokenHash = tokenHash },
            cancellationToken: cancellationToken));
    }

    public async Task RevokeAsync(long id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  dbo.RefreshTokens
            SET     RevokedAt = SYSUTCDATETIME()
            WHERE   Id = @Id AND RevokedAt IS NULL;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
