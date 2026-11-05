using CordBackend.Data;
using CordBackend.DTOs;
using CordBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Route = CordBackend.Models.Route;

namespace CordBackend.Controllers;

[ApiController]
[Route("api/coordinates")]
[Authorize]
public sealed class CoordinateController(CordDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CoordinateExportDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinateExportDTO>> GetCoordinates(
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken);
        if (user is null)
        {
            return User.FindFirst("sub")?.Value is null
                ? Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Google subject claim is missing."
                })
                : NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "User has not been registered."
                });
        }
        return Ok(await GetCoordinateExportAsync(user.Id, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<CoordinateExportDTO>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CoordinateExportDTO>> CreateCoordinates(
        [FromBody] CoordinateExportDTO export,
        CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken);
        if (user is null)
        {
            return User.FindFirst("sub")?.Value is null
                ? Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Google subject claim is missing."
                })
                : NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "User has not been registered."
                });
        }

        var incomingCoordinate = export.Coordinates
        .DistinctBy(dto => (dto.Latitude, dto.Longitude))
        .ToList();
        var incomingLatitudes = incomingCoordinate.Select(dto => dto.Latitude).ToList();
        var existingCoordinates = await dbContext.Coordinates
            .AsNoTracking()
            .Where(coordinate => coordinate.UserId == user.Id
                && incomingLatitudes.Contains(coordinate.Latitude))
            .Select(coordinate => new { coordinate.Latitude, coordinate.Longitude })
            .ToListAsync(cancellationToken);
        var coordinateSet = existingCoordinates
            .Select(coordinate => (coordinate.Latitude, coordinate.Longitude))
            .ToHashSet();

        var coordinates = incomingCoordinate
            .Where(dto => coordinateSet.Add((dto.Latitude, dto.Longitude)))
            .Select(dto => new Coordinate
            {
                UserId = user.Id,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Date = dto.Date
            })
            .ToList();

        var routes = export.Routes.Select(routeDto =>
        {
            var route = new Route
            {
                UserId = user.Id,
                Name = routeDto.Name,
                Date = routeDto.Date
            };

            foreach (var coordinateDto in routeDto.Coordinates)
            {
                route.Coordinates.Add(new Coordinate
                {
                    UserId = user.Id,
                    Latitude = coordinateDto.Latitude,
                    Longitude = coordinateDto.Longitude,
                    Date = coordinateDto.Date,
                    Route = route
                });
            }

            return route;
        }).ToList();

        dbContext.Coordinates.AddRange(coordinates);
        dbContext.Routes.AddRange(routes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok(await GetCoordinateExportAsync(user.Id, cancellationToken));
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCordinates(CancellationToken cancellationToken)
    {
        var user = await GetAuthenticatedUserAsync(cancellationToken);
        if(user is null)
        {
            return User.FindFirst("sub")?.Value is null
                ? Unauthorized(new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Google subject claim is missing."
                })
                : NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "User has not been registered."
                });
        }
        var coordinates = await dbContext.Coordinates
            .Where(coordinate => coordinate.UserId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.Coordinates.RemoveRange(coordinates);
        var routes = await dbContext.Routes
            .Where(route => route.UserId == user.Id)
            .ToListAsync(cancellationToken);
        dbContext.Routes.RemoveRange(routes);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
    }

    private Task<User?> GetAuthenticatedUserAsync(CancellationToken cancellationToken)
    {
        var googleSubject = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(googleSubject))
        {
            return Task.FromResult<User?>(null);
        }

        return dbContext.Users.SingleOrDefaultAsync(
            user => user.GoogleSubject == googleSubject,
            cancellationToken);
    }

    private async Task<CoordinateExportDTO> GetCoordinateExportAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var standaloneCoordinates = await dbContext.Coordinates
            .AsNoTracking()
            .Where(coordinate => coordinate.UserId == userId && coordinate.RouteId == null)
            .Select(coordinate => new CoordinateDTO
            {
                Latitude = coordinate.Latitude,
                Longitude = coordinate.Longitude,
                Date = coordinate.Date
            })
            .ToListAsync(cancellationToken);

        var routes = await dbContext.Routes
            .AsNoTracking()
            .Where(route => route.UserId == userId)
            .Select(route => new RouteDTO
            {
                Name = route.Name,
                Date = route.Date,
                Coordinates = route.Coordinates
                    .OrderBy(coordinate => coordinate.Id)
                    .Select(coordinate => new CoordinateDTO
                    {
                        Latitude = coordinate.Latitude,
                        Longitude = coordinate.Longitude,
                        Date = coordinate.Date
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new CoordinateExportDTO
        {
            Coordinates = standaloneCoordinates,
            Routes = routes
        };
    }
}
