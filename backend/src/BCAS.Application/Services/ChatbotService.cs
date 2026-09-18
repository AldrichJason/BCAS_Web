using BCAS.Application.Abstractions;
using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Domain.Entities;

namespace BCAS.Application.Services;

public interface IChatbotService
{
    Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FaqResponse>> GetFaqsAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<FaqResponse> CreateFaqAsync(FaqRequest request, CancellationToken cancellationToken = default);

    Task<FaqResponse> UpdateFaqAsync(int id, FaqRequest request, CancellationToken cancellationToken = default);

    Task DeleteFaqAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Rule based assistant: the visitor's message is scored against the FAQ knowledge base
/// kept in SQL Server, so the portal needs no external AI service to answer common questions.
/// </summary>
public class ChatbotService : IChatbotService
{
    private const string FallbackReply =
        "Sorry, I don't have an answer for that yet. You may reach the BCAS registrar at " +
        "registrar@bcas.edu.ph, or try one of the questions below.";

    /// <summary>Minimum share of the visitor's words that must match before an answer is used.</summary>
    private const double MatchThreshold = 0.34;

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "about", "an", "and", "are", "as", "at", "be", "can", "do", "does", "for", "from", "how",
        "i", "in", "is", "it", "me", "my", "of", "on", "or", "please", "the", "there", "this", "to",
        "was", "what", "when", "where", "which", "who", "why", "will", "with", "you", "your"
    };

    private readonly IFaqRepository _faqs;
    private readonly IChatMessageRepository _chatMessages;
    private readonly IClock _clock;

    public ChatbotService(IFaqRepository faqs, IChatMessageRepository chatMessages, IClock clock)
    {
        _faqs = faqs;
        _chatMessages = chatMessages;
        _clock = clock;
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var message = request.Message.Trim();
        if (message.Length == 0)
        {
            throw new ValidationFailedException("Please type a question first.");
        }

        var sessionId = request.SessionId ?? Guid.NewGuid();
        var faqs = await _faqs.GetAllAsync(true, cancellationToken);
        var tokens = Tokenise(message);

        Faq? best = null;
        var bestScore = 0d;

        foreach (var faq in faqs)
        {
            var score = Score(tokens, faq);
            if (score > bestScore)
            {
                bestScore = score;
                best = faq;
            }
        }

        var matched = bestScore >= MatchThreshold ? best : null;
        var reply = matched?.Answer ?? FallbackReply;

        await _chatMessages.InsertAsync(
            new ChatMessage
            {
                SessionId = sessionId,
                UserMessage = message,
                BotReply = reply,
                MatchedFaqId = matched?.Id,
                CreatedAt = _clock.UtcNow
            },
            cancellationToken);

        return new ChatResponse
        {
            SessionId = sessionId,
            Reply = reply,
            MatchedFaqId = matched?.Id,
            Suggestions = matched is null
                ? faqs.Take(3).Select(f => f.Question).ToList()
                : Array.Empty<string>()
        };
    }

    public async Task<IReadOnlyList<FaqResponse>> GetFaqsAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var faqs = await _faqs.GetAllAsync(includeInactive ? null : true, cancellationToken);
        return faqs.Select(ToResponse).ToList();
    }

    public async Task<FaqResponse> CreateFaqAsync(FaqRequest request, CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;
        var faq = new Faq
        {
            Question = request.Question.Trim(),
            Answer = request.Answer.Trim(),
            Keywords = request.Keywords.Trim(),
            Category = request.Category.Trim(),
            IsActive = request.IsActive,
            CreatedAt = now,
            UpdatedAt = now
        };

        faq.Id = await _faqs.InsertAsync(faq, cancellationToken);

        return ToResponse(faq);
    }

    public async Task<FaqResponse> UpdateFaqAsync(int id, FaqRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _faqs.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("FAQ", id);

        existing.Question = request.Question.Trim();
        existing.Answer = request.Answer.Trim();
        existing.Keywords = request.Keywords.Trim();
        existing.Category = request.Category.Trim();
        existing.IsActive = request.IsActive;
        existing.UpdatedAt = _clock.UtcNow;

        if (!await _faqs.UpdateAsync(existing, cancellationToken))
        {
            throw new NotFoundException("FAQ", id);
        }

        return ToResponse(existing);
    }

    public async Task DeleteFaqAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _faqs.DeleteAsync(id, cancellationToken))
        {
            throw new NotFoundException("FAQ", id);
        }
    }

    /// <summary>
    /// Share of the visitor's meaningful words that appear in the entry's keywords or question,
    /// with an extra weighting for the curated keyword list.
    /// </summary>
    internal static double Score(IReadOnlyCollection<string> messageTokens, Faq faq)
    {
        if (messageTokens.Count == 0)
        {
            return 0;
        }

        var keywordTokens = Tokenise(faq.Keywords.Replace(',', ' '));
        var questionTokens = Tokenise(faq.Question);

        var hits = 0d;

        foreach (var token in messageTokens)
        {
            if (keywordTokens.Contains(token))
            {
                hits += 1;
            }
            else if (questionTokens.Contains(token))
            {
                hits += 0.6;
            }
        }

        return hits / messageTokens.Count;
    }

    internal static HashSet<string> Tokenise(string value)
    {
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in value.Split(
                     new[] { ' ', '\t', '\n', '\r', ',', '.', '?', '!', ';', ':', '(', ')', '"', '\'', '/' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var token = raw.ToLowerInvariant();

            if (token.Length < 2 || StopWords.Contains(token))
            {
                continue;
            }

            tokens.Add(token);
        }

        return tokens;
    }

    private static FaqResponse ToResponse(Faq faq) => new()
    {
        Id = faq.Id,
        Question = faq.Question,
        Answer = faq.Answer,
        Keywords = faq.Keywords,
        Category = faq.Category,
        IsActive = faq.IsActive
    };
}
