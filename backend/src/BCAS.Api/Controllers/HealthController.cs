using BCAS.Infrastructure.Persistence;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BCAS.Api.Controllers;

/// <summary>Liveness and database connectivity probe.</summary>
[Route("api/health")]
[AllowAnonymous]
public class HealthController : ApiControllerBase
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IDbConnectionFactory connectionFactory, ILogger<HealthController> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                "SELECT 1;",
                cancellationToken: cancellationToken));

            return Ok(new { status = "healthy", database = "up" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "degraded", database = "down" });
        }
    }
}
