using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class ProgramRepository : IProgramRepository
{
    private const string BaseSelect = """
        SELECT  Id, Code, Name, Slug, Description, DegreeLevel, DurationYears,
                IsActive, DisplayOrder, CreatedAt, UpdatedAt
        FROM    dbo.Programs
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public ProgramRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<AcademicProgram>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            {BaseSelect}
            WHERE   (@ActiveOnly IS NULL OR IsActive = @ActiveOnly)
            ORDER BY DisplayOrder, Name;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var programs = await connection.QueryAsync<AcademicProgram>(new CommandDefinition(
            sql,
            new { ActiveOnly = activeOnly },
            cancellationToken: cancellationToken));

        return programs.ToList();
    }

    public async Task<AcademicProgram?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AcademicProgram>(new CommandDefinition(
            $"{BaseSelect} WHERE Slug = @Slug;",
            new { Slug = slug },
            cancellationToken: cancellationToken));
    }

    public async Task<AcademicProgram?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<AcademicProgram>(new CommandDefinition(
            $"{BaseSelect} WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.Programs
                        WHERE Slug = @Slug AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                    ) THEN 1 ELSE 0 END;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { Slug = slug, ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    public async Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT  CASE WHEN EXISTS (
                        SELECT 1 FROM dbo.Programs
                        WHERE Code = @Code AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
                    ) THEN 1 ELSE 0 END;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql,
            new { Code = code, ExcludeId = excludeId },
            cancellationToken: cancellationToken));
    }

    public async Task<int> InsertAsync(AcademicProgram program, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO dbo.Programs
                (Code, Name, Slug, Description, DegreeLevel, DurationYears, IsActive, DisplayOrder, CreatedAt, UpdatedAt)
            VALUES
                (@Code, @Name, @Slug, @Description, @DegreeLevel, @DurationYears, @IsActive, @DisplayOrder, @CreatedAt, @UpdatedAt);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            program,
            cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(AcademicProgram program, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE  dbo.Programs
            SET     Code = @Code,
                    Name = @Name,
                    Slug = @Slug,
                    Description = @Description,
                    DegreeLevel = @DegreeLevel,
                    DurationYears = @DurationYears,
                    IsActive = @IsActive,
                    DisplayOrder = @DisplayOrder,
                    UpdatedAt = @UpdatedAt
            WHERE   Id = @Id;
            """;

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            program,
            cancellationToken: cancellationToken));

        return affected > 0;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var affected = await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM dbo.Programs WHERE Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));

        return affected > 0;
    }
}
