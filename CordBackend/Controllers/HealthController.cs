using CordBackend.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CordBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(
    CordDbContext dbContext,
    ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<DatabaseHealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<DatabaseHealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DatabaseHealthResponse>> Get(CancellationToken cancellationToken)
    {
        if (await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            return Ok(new DatabaseHealthResponse("Healthy", "Connected"));
        }

        logger.LogWarning("Database health check failed.");
        return StatusCode(
            StatusCodes.Status503ServiceUnavailable,
            new DatabaseHealthResponse("Unhealthy", "Unavailable"));
    }
}

/// <summary>Reports the API and PostgreSQL connectivity status.</summary>
public sealed record DatabaseHealthResponse(string Status, string Database);
