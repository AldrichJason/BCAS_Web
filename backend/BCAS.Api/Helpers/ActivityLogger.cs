using BCAS.Api.Repositories.Utilities;
using Dapper;

namespace BCAS.Api.Helpers;

/// <summary>
/// Writes to ops.ActivityLog. Logging is best-effort: a failure here must never
/// turn a successful request into an error, so exceptions are swallowed after
/// being written to the application log.
/// </summary>
public sealed class ActivityLogger : IActivityLogger
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ActivityLogger> _logger;

    public ActivityLogger(
        ISqlConnectionFactory connectionFactory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ActivityLogger> logger)
    {
        _connectionFactory = connectionFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(
        string action,
        int? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? detail = null,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO ops.ActivityLog (UserId, Action, EntityType, EntityId, Detail, IpAddress, UserAgent)
            VALUES (@UserId, @Action, @EntityType, @EntityId, @Detail, @IpAddress, @UserAgent);
            """;

        var http = _httpContextAccessor.HttpContext;
        var userAgent = http?.Request.Headers.UserAgent.ToString();

        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            var command = new CommandDefinition(
                sql,
                new
                {
                    UserId = userId,
                    Action = action,
                    EntityType = entityType,
                    EntityId = entityId,
                    Detail = detail,
                    IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = string.IsNullOrEmpty(userAgent) ? null : Truncate(userAgent, 400),
                },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write activity log entry for action {Action}.", action);
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
