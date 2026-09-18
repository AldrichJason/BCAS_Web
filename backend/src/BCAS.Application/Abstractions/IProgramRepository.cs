using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IProgramRepository
{
    Task<IReadOnlyList<AcademicProgram>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken = default);

    Task<AcademicProgram?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<AcademicProgram?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, int? excludeId, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(AcademicProgram program, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(AcademicProgram program, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
