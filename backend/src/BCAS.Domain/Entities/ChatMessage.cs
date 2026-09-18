namespace BCAS.Domain.Entities;

public class ChatMessage
{
    public long Id { get; set; }
    public Guid SessionId { get; set; }
    public string UserMessage { get; set; } = string.Empty;
    public string BotReply { get; set; } = string.Empty;
    public int? MatchedFaqId { get; set; }
    public DateTime CreatedAt { get; set; }
}
