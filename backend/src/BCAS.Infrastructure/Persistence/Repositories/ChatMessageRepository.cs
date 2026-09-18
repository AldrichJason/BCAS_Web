using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class ChatMessageRepository : IChatMessageRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public ChatMessageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ChatMessages (SessionId, UserMessage, BotReply, MatchedFaqId, CreatedAt)
            VALUES (@SessionId, @UserMessage, @BotReply, @MatchedFaqId, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<long>(new CommandDefinition(
            sql,
            message,
            cancellationToken: cancellationToken));
    }
}
