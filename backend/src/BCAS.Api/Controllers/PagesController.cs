using BCAS.Application.Dtos;
using BCAS.Application.Services;
using BCAS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

/// <summary>Editable static pages such as "About BCAS" or "Admission".</summary>
[Route("api/pages")]
public class PagesController : ApiControllerBase
{
    private readonly IPageService _pages;

    public PagesController(IPageService pages)
    {
        _pages = pages;
    }

    /// <summary>Lists the content pages.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PageResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PageResponse>>> GetAll(
        [FromQuery] bool includeDrafts,
        CancellationToken cancellationToken) =>
        Ok(await _pages.GetAllAsync(includeDrafts && CanManageContent, cancellationToken));

    /// <summary>Reads a single page by its slug.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResponse>> GetBySlug(string slug, CancellationToken cancellationToken) =>
        Ok(await _pages.GetBySlugAsync(slug, CanManageContent, cancellationToken));

    /// <summary>Creates a page.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(PageResponse), StatusCodes.Status201Created)]
    public async Task<ActionResult<PageResponse>> Create(PageRequest request, CancellationToken cancellationToken)
    {
        var created = await _pages.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    /// <summary>Updates an existing page.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(PageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResponse>> Update(
        int id,
        PageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _pages.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a page.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _pages.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
