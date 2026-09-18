using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Domain.Entities;

namespace BCAS.Application.Services;

public interface IProgramService
{
    Task<IReadOnlyList<ProgramResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<ProgramResponse> GetBySlugAsync(string slug, bool includeInactive, CancellationToken cancellationToken = default);

    Task<ProgramResponse> CreateAsync(ProgramRequest request, CancellationToken cancellationToken = default);

    Task<ProgramResponse> UpdateAsync(int id, ProgramRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public class ProgramService : IProgramService
{
    private readonly IProgramRepository _repository;
    private readonly IClock _clock;

    public ProgramService(IProgramRepository repository, IClock clock)
    {
        _repository = repository;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ProgramResponse>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var programs = await _repository.GetAllAsync(includeInactive ? null : true, cancellationToken);
        return programs.Select(ToResponse).ToList();
    }

    public async Task<ProgramResponse> GetBySlugAsync(string slug, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var program = await _repository.GetBySlugAsync(slug, cancellationToken);

        if (program is null || (!program.IsActive && !includeInactive))
        {
            throw new NotFoundException("Program", slug);
        }

        return ToResponse(program);
    }

    public async Task<ProgramResponse> CreateAsync(ProgramRequest request, CancellationToken cancellationToken = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();

        if (await _repository.CodeExistsAsync(code, null, cancellationToken))
        {
            throw new ConflictException($"A program with code '{code}' already exists.");
        }

        var now = _clock.UtcNow;
        var program = new AcademicProgram
        {
            Code = code,
            Name = request.Name.Trim(),
            Slug = await BuildUniqueSlugAsync(request.Name, null, cancellationToken),
            Description = request.Description,
            DegreeLevel = request.DegreeLevel.Trim(),
            DurationYears = request.DurationYears,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = now,
            UpdatedAt = now
        };

        program.Id = await _repository.InsertAsync(program, cancellationToken);

        return ToResponse(program);
    }

    public async Task<ProgramResponse> UpdateAsync(int id, ProgramRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Program", id);

        var code = request.Code.Trim().ToUpperInvariant();

        if (await _repository.CodeExistsAsync(code, id, cancellationToken))
        {
            throw new ConflictException($"A program with code '{code}' already exists.");
        }

        var nameChanged = !string.Equals(existing.Name, request.Name.Trim(), StringComparison.Ordinal);

        existing.Code = code;
        existing.Name = request.Name.Trim();
        existing.Slug = nameChanged
            ? await BuildUniqueSlugAsync(request.Name, id, cancellationToken)
            : existing.Slug;
        existing.Description = request.Description;
        existing.DegreeLevel = request.DegreeLevel.Trim();
        existing.DurationYears = request.DurationYears;
        existing.IsActive = request.IsActive;
        existing.DisplayOrder = request.DisplayOrder;
        existing.UpdatedAt = _clock.UtcNow;

        if (!await _repository.UpdateAsync(existing, cancellationToken))
        {
            throw new NotFoundException("Program", id);
        }

        return ToResponse(existing);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _repository.DeleteAsync(id, cancellationToken))
        {
            throw new NotFoundException("Program", id);
        }
    }

    private async Task<string> BuildUniqueSlugAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var baseSlug = Slug.From(name);
        if (string.IsNullOrEmpty(baseSlug))
        {
            throw new ValidationFailedException("The program name must contain at least one letter or digit.");
        }

        var candidate = baseSlug;
        var suffix = 2;

        while (await _repository.SlugExistsAsync(candidate, excludeId, cancellationToken))
        {
            candidate = $"{baseSlug}-{suffix++}";
        }

        return candidate;
    }

    private static ProgramResponse ToResponse(AcademicProgram program) => new()
    {
        Id = program.Id,
        Code = program.Code,
        Name = program.Name,
        Slug = program.Slug,
        Description = program.Description,
        DegreeLevel = program.DegreeLevel,
        DurationYears = program.DurationYears,
        IsActive = program.IsActive,
        DisplayOrder = program.DisplayOrder
    };
}
