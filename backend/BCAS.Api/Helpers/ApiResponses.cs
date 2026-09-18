namespace BCAS.Api.Helpers;

/// <summary>Error shape returned by the API. Messages are safe to show to users.</summary>
/// <param name="Code">Stable machine-readable code, e.g. "invalid_credentials".</param>
/// <param name="Message">Human-readable message.</param>
public sealed record ApiError(string Code, string Message);

/// <summary>A plain informational response carrying only a display message.</summary>
/// <param name="Message">Human-readable message.</param>
public sealed record ApiMessage(string Message);
