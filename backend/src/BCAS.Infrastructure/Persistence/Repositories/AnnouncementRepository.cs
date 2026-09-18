using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class AnnouncementRepository : IAnnouncementRepository
{
    private const string BaseSelect = """
        SELECT  a.Id, a.Title, a.Slug, a.Summary, a.Content, a.ImageUrl, a.Category,
                a.IsPublished, a.PublishedAt, a.AuthorId, u.FullName AS AuthorName,
                a.CreatedAt, a.UpdatedAt
        FROM    dbo.Announcements a
        JOIN    dbo.Users u ON u.Id = a.AuthorId
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public AnnouncementRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<PagedResult<Announcement>> GetPagedAsync(
        PageQuery query,
        string? category,
        bool? publishedOnly,
        CancellationToken cancellationToken = default)
    {
        // @PublishedOnly / @Category are optional: a NULL value disables that filter.
        const string filter = """
            WHERE   (@PublishedOnly IS NULL OR a.IsPublished = @PublishedOnly)
            AND     (@Category IS NULL OR a.Category = @Category)
            AND     (@Search IS NULL OR a.Title LIKE @SearchPattern OR a.Summary LIKE @SearchPattern)
            """;

        var sql = $"""
            {BaseSelect}
            {filter}
            ORDER BY COALESCE(a.PublishedAt, a.CreatedAt) DESC, a.Id DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

            SELECT  COUNT(1)
            FROM    dbo.Announcements a
            {filter};
            """;

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var parameters = new
        {
            PublishedOnly = publishedOnly,
            Category = category,
            Search = search,
            SearchPattern = search is null ? null : $"%{Escape(search)}%",
            query.Offset,
            query.PageSize
        };

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken));

        var items = (await multi.ReadAsync<Announcement>()).ToList();
        var total = await multi.ReadSingleAsync<int>();

        return PagedResult<Announcement>.Create(items, query.Page, query.PageSize, total);
    }

    public async Task<Announcement?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Announcement>(new CommandDefinition(
            $"{BaseSelect} WHERE a.Slug = @Slug;",
            new { Slug = slug },
            cancellationToken: cancellationToken));
    }

    public async Task<Announcement?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<Announcement>(new CommandDefinition(
            $"{BaseSelect} WHERE a.Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.Announcements
                        WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                    ) THEN 1 ELSE 0 END;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { Slug = slug, ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    public async Task<int> InsertAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Announcements
                (Title, Slug, Summary, Content, ImageUrl, Category, IsPublished, PublishedAt, AuthorId, CreatedAt, UpdatedAt)
            VALUES
                (@Title, @Slug, @Summary, @Content, @ImageUrl, @Category, @IsPublished, @PublishedAt, @AuthorId, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            announcement,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  dbo.Announcements
            SET     Title = @Title,
                    Slug = @Slug,
                    Summary = @Summary,
                    Content = @Content,
                    ImageUrl = @ImageUrl,
                    Category = @Category,
                    IsPublished = @IsPublished,
                    PublishedAt = @PublishedAt,
                    UpdatedAt = @UpdatedAt
            WHERE   Id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            announcement,
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Announcements WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT DISTINCT Category
            FROM   dbo.Announcements
            WHERE  IsPublished = 1
            ORDER BY Category;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var categories = await connection.QueryAsync<string>(new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

        return categories.ToList();
    }

    /// <summary>Escapes the LIKE wildcards so a search for "50%" does not match everything.</summary>
    private static string Escape(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
