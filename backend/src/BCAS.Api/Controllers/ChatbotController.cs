using BCAS.Application.Dtos;
using BCAS.Application.Services;
using BCAS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

/// <summary>The portal assistant and the FAQ knowledge base behind it.</summary>
[Route("api/chatbot")]
public class ChatbotController : ApiControllerBase
{
    private readonly IChatbotService _chatbot;

    public ChatbotController(IChatbotService chatbot)
    {
        _chatbot = chatbot;
    }

    /// <summary>Answers a visitor question from the FAQ knowledge base.</summary>
    [HttpPost("ask")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ChatResponse>> Ask(ChatRequest request, CancellationToken cancellationToken) =>
        Ok(await _chatbot.AskAsync(request, cancellationToken));

    /// <summary>Lists the FAQ entries.</summary>
    [HttpGet("faqs")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<FaqResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FaqResponse>>> GetFaqs(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken) =>
        Ok(await _chatbot.GetFaqsAsync(includeInactive && CanManageContent, cancellationToken));

    /// <summary>Adds a FAQ entry to the knowledge base.</summary>
    [HttpPost("faqs")]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<FaqResponse>> CreateFaq(FaqRequest request, CancellationToken cancellationToken)
    {
        var created = await _chatbot.CreateFaqAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetFaqs), new { id = created.Id }, created);
    }

    /// <summary>Updates a FAQ entry.</summary>
    [HttpPut("faqs/{id:int}")]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(FaqResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FaqResponse>> UpdateFaq(
        int id,
        FaqRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _chatbot.UpdateFaqAsync(id, request, cancellationToken));

    /// <summary>Deletes a FAQ entry.</summary>
    [HttpDelete("faqs/{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteFaq(int id, CancellationToken cancellationToken)
    {
        await _chatbot.DeleteFaqAsync(id, cancellationToken);

        return NoContent();
    }
}
