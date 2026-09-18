using BCAS.Application.Abstractions;
using BCAS.Domain.Entities;

namespace BCAS.UnitTests;

public class FixedClock : IClock
{
    public FixedClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; set; }
}

public class InMemoryFaqRepository : IFaqRepository
{
    private readonly List<Faq> _faqs;

    public InMemoryFaqRepository(params Faq[] faqs) => _faqs = faqs.ToList();

    public Task<IReadOnlyList<Faq>> GetAllAsync(bool? activeOnly, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Faq> result = activeOnly is null
            ? _faqs
            : _faqs.Where(f => f.IsActive == activeOnly).ToList();

        return Task.FromResult(result);
    }

    public Task<Faq?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_faqs.SingleOrDefault(f => f.Id == id));

    public Task<int> InsertAsync(Faq faq, CancellationToken cancellationToken = default)
    {
        faq.Id = _faqs.Count == 0 ? 1 : _faqs.Max(f => f.Id) + 1;
        _faqs.Add(faq);

        return Task.FromResult(faq.Id);
    }

    public Task<bool> UpdateAsync(Faq faq, CancellationToken cancellationToken = default) =>
        Task.FromResult(_faqs.Any(f => f.Id == faq.Id));

    public Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_faqs.RemoveAll(f => f.Id == id) > 0);
}

public class InMemoryChatMessageRepository : IChatMessageRepository
{
    public List<ChatMessage> Messages { get; } = new();

    public Task<long> InsertAsync(ChatMessage message, CancellationToken cancellationToken = default)
    {
        message.Id = Messages.Count + 1;
        Messages.Add(message);

        return Task.FromResult(message.Id);
    }
}
