using BCAS.Application.Common;
using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IAnnouncementRepository
{
    Task<PagedResult<Announcement>> GetPagedAsync(
        PageQuery query,
        string? category,
        bool? publishedOnly,
        CancellationToken cancellationToken = default);

    Task<Announcement?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<Announcement?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Announcement announcement, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Announcement announcement, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
