using BCAS.Application.Common;
using BCAS.Application.Dtos;
using BCAS.Application.Services;
using BCAS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

/// <summary>News and announcements shown on the public portal and edited in the CMS.</summary>
[Route("api/announcements")]
public class AnnouncementsController : ApiControllerBase
{
    private readonly IAnnouncementService _announcements;

    public AnnouncementsController(IAnnouncementService announcements)
    {
        _announcements = announcements;
    }

    /// <summary>Lists announcements, newest first. Drafts are only returned to CMS users.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<AnnouncementResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<AnnouncementResponse>>> GetAll(
        [FromQuery] PageQuery query,
        [FromQuery] string? category,
        [FromQuery] bool includeDrafts,
        CancellationToken cancellationToken) =>
        Ok(await _announcements.GetPagedAsync(query, category, includeDrafts && CanManageContent, cancellationToken));

    /// <summary>The distinct categories currently in use by published announcements.</summary>
    [HttpGet("categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<string>>> GetCategories(CancellationToken cancellationToken) =>
        Ok(await _announcements.GetCategoriesAsync(cancellationToken));

    /// <summary>Reads a single announcement by its slug.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementResponse>> GetBySlug(string slug, CancellationToken cancellationToken) =>
        Ok(await _announcements.GetBySlugAsync(slug, CanManageContent, cancellationToken));

    /// <summary>Creates an announcement.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<AnnouncementResponse>> Create(
        AnnouncementRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _announcements.CreateAsync(request, CurrentUserId, cancellationToken);

        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    /// <summary>Updates an existing announcement.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(AnnouncementResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AnnouncementResponse>> Update(
        int id,
        AnnouncementRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _announcements.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes an announcement.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _announcements.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
