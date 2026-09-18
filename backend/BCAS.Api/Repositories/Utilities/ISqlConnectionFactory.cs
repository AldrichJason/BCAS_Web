using System.Data;

namespace BCAS.Api.Repositories.Utilities;

public interface ISqlConnectionFactory
{
    /// <summary>Creates a new, already-open connection to the BCAS database.</summary>
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
