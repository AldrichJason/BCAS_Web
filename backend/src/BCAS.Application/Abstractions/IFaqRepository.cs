using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IFaqRepository
{
    Task<IReadOnlyList<Faq>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken = default);

    Task<Faq?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Faq faq, CancellationToken cancellationToken = default);

    Task<bool> UpdateAsync(Faq faq, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
