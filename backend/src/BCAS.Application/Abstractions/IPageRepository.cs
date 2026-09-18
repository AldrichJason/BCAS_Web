using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IPageRepository
{
    Task<IReadOnlyList<ContentPage>> GetAllAsync(bool? publishedOnly, CancellationToken cancellationToken = default);

    Task<ContentPage?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<ContentPage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(ContentPage page, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(ContentPage page, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
