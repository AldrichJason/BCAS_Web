using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Domain.Entities;

namespace BCAS.Application.Services;

public interface IAnnouncementService
{
    Task<PagedResult<AnnouncementResponse>> GetPagedAsync(
        PageQuery query,
        string? category,
        bool includeUnpublished,
        CancellationToken cancellationToken = default);

    Task<AnnouncementResponse> GetBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken = default);

    Task<AnnouncementResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<AnnouncementResponse> CreateAsync(AnnouncementRequest request, int authorId, CancellationToken cancellationToken = default);

    Task<AnnouncementResponse> UpdateAsync(int id, AnnouncementRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class AnnouncementService : IAnnouncementService
{
    private readonly IAnnouncementRepository _repository;
    private readonly IClock _clock;

    public AnnouncementService(IAnnouncementRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<PagedResult<AnnouncementResponse>> GetPagedAsync(
        PageQuery query,
        string? category,
        bool includeUnpublished,
        CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetPagedAsync(
            query,
            string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            includeUnpublished ? null : true,
            cancellationToken);

        return PagedResult<AnnouncementResponse>.Create(
            page.Items.Select(ToResponse).ToList(),
            page.Page,
            page.PageSize,
            page.TotalCount);
    }

    public async Task<AnnouncementResponse> GetBySlugAsync(string slug, bool includeUnpublished, CancellationToken cancellationToken = default)
    {
        var announcement = await _repository.GetBySlugAsync(slug, cancellationToken);

        if (announcement is null || (!announcement.IsPublished && !includeUnpublished))
        {
            throw new NotFoundException("Announcement", slug);
        }

        return ToResponse(announcement);
    }

    public async Task<AnnouncementResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var announcement = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Announcement", id);

        return ToResponse(announcement);
    }

    public Task<IReadOnlyList<string>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        _repository.GetCategoriesAsync(cancellationToken);

    public async Task<AnnouncementResponse> CreateAsync(AnnouncementRequest request, int authorId, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var announcement = new Announcement
        {
            Title = request.Title.Trim(),
            Slug = await BuildUniqueSlugAsync(request.Title, null, cancellationToken),
            Summary = request.Summary.Trim(),
            Content = request.Content,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Category = request.Category.Trim(),
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? now : null,
            AuthorId = authorId,
            CreatedAt = now,
            UpdatedAt = now
        };

        announcement.Id = await _repository.InsertAsync(announcement, cancellationToken);

        return await GetByIdAsync(announcement.Id, cancellationToken);
    }

    public async Task<AnnouncementResponse> UpdateAsync(int id, AnnouncementRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Announcement", id);

        var titleChanged = !string.Equals(existing.Title, request.Title.Trim(), StringComparison.Ordinal);

        existing.Title = request.Title.Trim();
        existing.Slug = titleChanged
            ? await BuildUniqueSlugAsync(request.Title, id, cancellationToken)
            : existing.Slug;
        existing.Summary = request.Summary.Trim();
        existing.Content = request.Content;
        existing.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        existing.Category = request.Category.Trim();
        existing.UpdatedAt = _clock.UtcNow;

        // The publish date is stamped the first time an item goes live and kept afterwards.
        if (request.IsPublished && !existing.IsPublished)
        {
            existing.PublishedAt = _clock.UtcNow;
        }

        existing.IsPublished = request.IsPublished;

        if (!await _repository.UpdateAsync(existing, cancellationToken))
        {
            throw new NotFoundException("Announcement", id);
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _repository.DeleteAsync(id, cancellationToken))
        {
            throw new NotFoundException("Announcement", id);
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

    private static AnnouncementResponse ToResponse(Announcement announcement) => new()
    {
        Id = announcement.Id,
        Title = announcement.Title,
        Slug = announcement.Slug,
        Summary = announcement.Summary,
        Content = announcement.Content,
        ImageUrl = announcement.ImageUrl,
        Category = announcement.Category,
        IsPublished = announcement.IsPublished,
        PublishedAt = announcement.PublishedAt,
        AuthorName = announcement.AuthorName,
        CreatedAt = announcement.CreatedAt,
        UpdatedAt = announcement.UpdatedAt
    };
}
