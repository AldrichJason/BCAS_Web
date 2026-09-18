namespace BCAS.Api.Helpers;

public interface IActivityLogger
{
    Task LogAsync(
        string action,
        int? userId = null,
        string? entityType = null,
        string? entityId = null,
        string? detail = null,
        CancellationToken cancellationToken = default);
}
