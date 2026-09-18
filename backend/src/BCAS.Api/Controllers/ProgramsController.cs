using BCAS.Application.Dtos;
using BCAS.Application.Services;
using BCAS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

/// <summary>Academic programs offered by the college.</summary>
[Route("api/programs")]
public class ProgramsController : ApiControllerBase
{
    private readonly IProgramService _programs;

    public ProgramsController(IProgramService programs)
    {
        _programs = programs;
    }

    /// <summary>Lists programs ordered by their display order.</summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<ProgramResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProgramResponse>>> GetAll(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken) =>
        Ok(await _programs.GetAllAsync(includeInactive && CanManageContent, cancellationToken));

    /// <summary>Reads a single program by its slug.</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ProgramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> GetBySlug(string slug, CancellationToken cancellationToken) =>
        Ok(await _programs.GetBySlugAsync(slug, CanManageContent, cancellationToken));

    /// <summary>Creates a program.</summary>
    [HttpPost]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(ProgramResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramResponse>> Create(ProgramRequest request, CancellationToken cancellationToken)
    {
        var created = await _programs.CreateAsync(request, cancellationToken);

        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    /// <summary>Updates an existing program.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.AdministratorOrEditor)]
    [ProducesResponseType(typeof(ProgramResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramResponse>> Update(
        int id,
        ProgramRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _programs.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a program.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _programs.DeleteAsync(id, cancellationToken);

        return NoContent();
    }
}
