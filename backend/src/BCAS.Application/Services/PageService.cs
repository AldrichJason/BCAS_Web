using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Domain.Entities;

namespace BCAS.Application.Services;

public interface IPageService
{
    Task<IReadOnlyList<PageResponse>> GetAllAsync(bool includeUnpublished, CancellationToken cancellationToken = default);

    Task<PageResponse> GetBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken = default);

    Task<PageResponse> CreateAsync(PageRequest request, CancellationToken cancellationToken = default);

    Task<PageResponse> UpdateAsync(int id, PageRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class PageService : IPageService
{
    private readonly IPageRepository _repository;
    private readonly IClock _clock;

    public PageService(IPageRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PageResponse>> GetAllAsync(bool includeUnpublished, CancellationToken cancellationToken = default)
    {
        var pages = await _repository.GetAllAsync(includeUnpublished ? null : true, cancellationToken);
        return pages.Select(ToResponse).ToList();
    }

    public async Task<PageResponse> GetBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetBySlugAsync(slug, cancellationToken);

        if (page is null || (!page.IsPublished && !includeUnpublished))
        {
            throw new NotFoundException("Page", slug);
        }

        return ToResponse(page);
    }

    public async Task<PageResponse> CreateAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var page = new ContentPage
        {
            Title = request.Title.Trim(),
            Slug = await BuildUniqueSlugAsync(request.Title, null, cancellationToken),
            Content = request.Content,
            IsPublished = request.IsPublished,
            CreatedAt = now,
            UpdatedAt = now
        };

        page.Id = await _repository.InsertAsync(page, cancellationToken);

        return ToResponse(page);
    }

    public async Task<PageResponse> UpdateAsync(int id, PageRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Page", id);

        var titleChanged = !string.Equals(existing.Title, request.Title.Trim(), StringComparison.Ordinal);

        existing.Title = request.Title.Trim();
        existing.Slug = titleChanged
            ? await BuildUniqueSlugAsync(request.Title, id, cancellationToken)
            : existing.Slug;
        existing.Content = request.Content;
        existing.IsPublished = request.IsPublished;
        existing.UpdatedAt = _clock.UtcNow;

        if (!await _repository.UpdateAsync(existing, cancellationToken))
        {
            throw new NotFoundException("Page", id);
        }

        return ToResponse(existing);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _repository.DeleteAsync(id, cancellationToken))
        {
            throw new NotFoundException("Page", id);
        }
    }

    private async Task<string> BuildUniqueSlugAsync(string title, int? excludeId, CancellationToken cancellationToken)
    {
        var baseSlug = Slug.From(title);
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new ValidationFailedException("The title must contain at least one letter or digit.");
        }

        var candidate = baseSlug;
        var suffix = 2;

        while (await _repository.SlugExistsAsync(candidate, excludeId, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        return candidate;
    }

    private static PageResponse ToResponse(ContentPage page) => new()
    {
        Id = page.Id,
        Slug = page.Slug,
        Title = page.Title,
        Content = page.Content,
        IsPublished = page.IsPublished,
        UpdatedAt = page.UpdatedAt
    };
}
