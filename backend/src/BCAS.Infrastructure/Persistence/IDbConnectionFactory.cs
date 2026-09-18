using System.Data;

namespace BCAS.Infrastructure.Persistence;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
