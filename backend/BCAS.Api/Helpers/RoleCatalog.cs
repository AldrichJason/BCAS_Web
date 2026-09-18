namespace BCAS.Api.Helpers;

/// <summary>
/// The role codes stored in auth.Roles. Kept as constants so authorization
/// attributes and validation refer to the same strings the database does.
/// </summary>
public static class RoleCodes
{
    public const string SuperAdmin = "SUPER_ADMIN";

    public const string AcademicHead = "ACADEMIC_HEAD";

    public const string Registrar = "REGISTRAR";

    public const string VpOperations = "VP_OPERATIONS";

    public static readonly IReadOnlySet<string> All =
        new HashSet<string>(StringComparer.Ordinal) { SuperAdmin, AcademicHead, Registrar, VpOperations };

    /// <summary>
    /// Only the Academic Head is scoped to a department; the other roles work
    /// school-wide, so a department must not be supplied for them (BW-14).
    /// </summary>
    public static bool RequiresDepartment(string roleCode) =>
        string.Equals(roleCode, AcademicHead, StringComparison.Ordinal);
}
