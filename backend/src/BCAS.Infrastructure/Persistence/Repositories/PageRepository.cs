using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class PageRepository : IPageRepository
{
    private const string BaseSelect = """
        SELECT  Id, Slug, Title, Content, IsPublished, CreatedAt, UpdatedAt
        FROM    dbo.ContentPages
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public PageRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ContentPage>> GetAllAsync(bool? publishedOnly, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {BaseSelect}
            WHERE   (@PublishedOnly IS NULL OR IsPublished = @PublishedOnly)
            ORDER BY Title;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var pages = await connection.QueryAsync<ContentPage>(new CommandDefinition(
            sql,
            new { PublishedOnly = publishedOnly },
            cancellationToken: cancellationToken));

        return pages.ToList();
    }

    public async Task<ContentPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ContentPage>(new CommandDefinition(
            $"{BaseSelect} WHERE Slug = @Slug;",
            new { Slug = slug },
            cancellationToken: cancellationToken));
    }

    public async Task<ContentPage?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ContentPage>(new CommandDefinition(
            $"{BaseSelect} WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.ContentPages
                        WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                    ) THEN 1 ELSE 0 END;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { Slug = slug, ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    public async Task<int> InsertAsync(ContentPage page, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.ContentPages (Slug, Title, Content, IsPublished, CreatedAt, UpdatedAt)
            VALUES (@Slug, @Title, @Content, @IsPublished, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            page,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(ContentPage page, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  dbo.ContentPages
            SET     Slug = @Slug,
                    Title = @Title,
                    Content = @Content,
                    IsPublished = @IsPublished,
                    UpdatedAt = @UpdatedAt
            WHERE   Id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            page,
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.ContentPages WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return affected > 0;
    }
}
