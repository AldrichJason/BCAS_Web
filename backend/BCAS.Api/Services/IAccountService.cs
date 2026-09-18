using BCAS.Api.Models;
using BCAS.Api.Models.DTOs;

namespace BCAS.Api.Services;

public interface IAccountService
{
    Task<IReadOnlyList<UserAccount>> ListAsync(CancellationToken cancellationToken = default);

    Task<AccountReference> GetReferenceAsync(CancellationToken cancellationToken = default);

    /// <summary>Provisions an account and emails its invitation link (BW-14).</summary>
    Task<CreateUserResult> CreateAsync(
        CreateUserRequest request, int createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>Activates or deactivates an account without deleting it (BW-15).</summary>
    Task<SetActivationResult> SetActivationAsync(
        int userId, bool isActive, int changedByUserId, CancellationToken cancellationToken = default);
}
