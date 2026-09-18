using System.Data;
using Microsoft.Data.SqlClient;

namespace BCAS.Api.Common.Db;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("BcasDb")
            ?? throw new InvalidOperationException(
                "Connection string 'BcasDb' is not configured. Set it through user secrets, " +
                "environment variables or the deployment secret store.");

        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Connection string 'BcasDb' is empty.");
        }
    }

    public async Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
