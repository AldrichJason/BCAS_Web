using BCAS.Domain.Entities;

namespace BCAS.Application.Abstractions;

public interface IChatMessageRepository
{
    Task<long> InsertAsync(ChatMessage message, CancellationToken cancellationToken = default);
}
