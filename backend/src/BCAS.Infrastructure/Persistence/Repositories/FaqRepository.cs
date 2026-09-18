using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class FaqRepository : IFaqRepository
{
    private const string BaseSelect = """
        SELECT  Id, Question, Answer, Keywords, Category, IsActive, CreatedAt, UpdatedAt
        FROM    dbo.Faqs
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public FaqRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<Faq>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {BaseSelect}
            WHERE   (@ActiveOnly IS NULL OR IsActive = @ActiveOnly)
            ORDER BY Category, Question;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var faqs = await connection.QueryAsync<Faq>(new CommandDefinition(
            sql,
            new { ActiveOnly = activeOnly },
            cancellationToken: cancellationToken));

        return faqs.ToList();
    }

    public async Task<Faq?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Faq>(new CommandDefinition(
            $"{BaseSelect} WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<int> InsertAsync(Faq faq, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Faqs (Question, Answer, Keywords, Category, IsActive, CreatedAt, UpdatedAt)
            VALUES (@Question, @Answer, @Keywords, @Category, @IsActive, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            faq,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Faq faq, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  dbo.Faqs
            SET     Question = @Question,
                    Answer = @Answer,
                    Keywords = @Keywords,
                    Category = @Category,
                    IsActive = @IsActive,
                    UpdatedAt = @UpdatedAt
            WHERE   Id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            faq,
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Faqs WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return affected > 0;
    }
}
