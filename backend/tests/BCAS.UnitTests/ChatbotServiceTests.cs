using BCAS.Application.Dtos;
using BCAS.Application.Services;
using BCAS.Domain.Entities;
using Xunit;

namespace BCAS.UnitTests;

public class ChatbotServiceTests
{
    private static readonly Faq EnrollmentFaq = new()
    {
        Id = 1,
        Question = "How do I enroll at BCAS?",
        Answer = "Visit the registrar's office with your Form 138 and PSA birth certificate.",
        Keywords = "enroll,enrollment,admission,requirements",
        Category = "Admission",
        IsActive = true
    };

    private static readonly Faq TuitionFaq = new()
    {
        Id = 2,
        Question = "How much is the tuition fee?",
        Answer = "Tuition depends on the program; the registrar publishes the current rates every term.",
        Keywords = "tuition,fee,payment,cost",
        Category = "Finance",
        IsActive = true
    };

    [Fact]
    public async Task AskAsync_answers_from_the_knowledge_base()
    {
        var log = new InMemoryChatMessageRepository();
        var service = CreateService(log);

        var response = await service.AskAsync(new ChatRequest { Message = "What are the enrollment requirements?" });

        Assert.Equal(EnrollmentFaq.Id, response.MatchedFaqId);
        Assert.Equal(EnrollmentFaq.Answer, response.Reply);
        Assert.Empty(response.Suggestions);
        Assert.Single(log.Messages);
    }

    [Fact]
    public async Task AskAsync_falls_back_with_suggestions_when_nothing_matches()
    {
        var log = new InMemoryChatMessageRepository();
        var service = CreateService(log);

        var response = await service.AskAsync(new ChatRequest { Message = "Do you sell pizza?" });

        Assert.Null(response.MatchedFaqId);
        Assert.NotEmpty(response.Suggestions);
        Assert.Contains("registrar@bcas.edu.ph", response.Reply);
    }

    [Fact]
    public async Task AskAsync_keeps_the_session_id_supplied_by_the_visitor()
    {
        var sessionId = Guid.NewGuid();
        var log = new InMemoryChatMessageRepository();
        var service = CreateService(log);

        var response = await service.AskAsync(new ChatRequest { SessionId = sessionId, Message = "tuition fee" });

        Assert.Equal(sessionId, response.SessionId);
        Assert.Equal(sessionId, log.Messages[0].SessionId);
    }

    private static ChatbotService CreateService(InMemoryChatMessageRepository log) =>
        new(new InMemoryFaqRepository(EnrollmentFaq, TuitionFaq), log, new FixedClock(new DateTime(2026, 1, 1)));
}
