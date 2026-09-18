using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;
using Dapper;

namespace BCAS.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private const string BaseSelect = """
        SELECT  u.Id, u.Email, u.PasswordHash, u.FullName, u.RoleId,
                r.Name AS RoleName, u.IsActive, u.CreatedAt, u.UpdatedAt
        FROM    dbo.Users u
        JOIN    dbo.Roles r ON r.Id = u.RoleId
        """;

    private readonly IDbConnectionFactory _connectionFactory;

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            $"{BaseSelect} WHERE u.Email = @Email;",
            new { Email = email },
            cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            $"{BaseSelect} WHERE u.Id = @Id;",
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
