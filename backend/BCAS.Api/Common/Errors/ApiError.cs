namespace BCAS.Api.Common.Errors;

/// <summary>Error shape returned by the API. Messages are safe to show to users.</summary>
/// <param name="Code">Stable machine-readable code, e.g. "invalid_credentials".</param>
/// <param name="Message">Human-readable message.</param>
public sealed record ApiError(string Code, string Message);
