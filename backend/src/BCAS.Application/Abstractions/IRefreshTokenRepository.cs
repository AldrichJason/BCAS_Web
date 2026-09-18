using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task RevokeAsync(long id, CancellationToken cancellationToken = default);
}
