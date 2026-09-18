using BCAS.Api.Models;
using BCAS.Api.Models.DTOs;

namespace BCAS.Api.Repositories;

public interface IAccountRepository
{
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserAccount?> FindAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<int?> FindRoleIdAsync(string roleCode, CancellationToken cancellationToken = default);

    Task<bool> DepartmentExistsAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the account and, for a department-scoped role, its scope row, in
    /// one transaction. Returns the new user id.
    /// </summary>
    Task<int> CreateAsync(
        CreateUserRequest request,
        int roleId,
        string passwordHash,
        int createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flips auth.Users.IsActive (BW-15). Nothing is deleted, so authored content,
    /// its attribution and the activity log all stay exactly as they were.
    /// </summary>
    Task SetActivationAsync(
        int userId, bool isActive, int changedByUserId, CancellationToken cancellationToken = default);

    /// <summary>Number of Super Admin accounts that are currently active.</summary>
    Task<int> CountActiveSuperAdminsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleOption>> ListRolesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentOption>> ListDepartmentsAsync(CancellationToken cancellationToken = default);
}
