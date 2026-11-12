using CordBackend.Data;
using CordBackend.DTOs;
using CordBackend.Models;
using CordBackend.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Route = CordBackend.Models.Route;

namespace CordBackend.Controllers;

[ApiController]
[Route("api/coordinates")]
[Authorize]
public sealed class CoordinateController(
    CordDbContext dbContext,
    IImageStorageClient imageStorageClient,
    ILogger<CoordinateController> logger) : ControllerBase
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
        return Ok(await GetCoordinateExportAsync(user, cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType<CreateCoordinatesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreateCoordinatesResponse>> CreateCoordinates(
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
            .Select(dto => CreateCoordinate(dto, user.Id, user.GoogleSubject))
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
                route.Coordinates.Add(CreateCoordinate(
                    coordinateDto,
                    user.Id,
                    user.GoogleSubject,
                    route));
            }

            return route;
        }).ToList();

        var imageIds = coordinates
            .Concat(routes.SelectMany(route => route.Coordinates))
            .Where(coordinate => coordinate.HasImage && coordinate.ImageId is not null)
            .Select(coordinate => coordinate.ImageId!)
            .ToList();
        IReadOnlyList<PresignedImageUpload> imageUploads = imageIds.Count == 0
            ? []
            : await imageStorageClient.CreatePresignedPutUrlsAsync(
                user.GoogleSubject,
                imageIds,
                cancellationToken);

        dbContext.Coordinates.AddRange(coordinates);
        dbContext.Routes.AddRange(routes);
        logger.LogInformation(
            "Saving {CoordinateCount} coordinates and {ImageCount} image URLs.",
            coordinates.Count + routes.Sum(route => route.Coordinates.Count),
            imageIds.Count);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation(
            "Saved {ImageCount} image URLs with the new coordinates.",
            imageIds.Count);

        var savedCoordinates = await GetCoordinateExportAsync(user, cancellationToken);
        return Ok(new CreateCoordinatesResponse
        {
            Coordinates = savedCoordinates.Coordinates,
            Routes = savedCoordinates.Routes,
            ImageUploads = imageUploads
                .Select(upload => new ImageUploadDTO
                {
                    ImageId = upload.ImageId,
                    Url = upload.Url,
                    ExpiresAtUtc = upload.ExpiresAtUtc
                })
                .ToList()
        });
    }

    [HttpPost("images/uploaded")] 
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkImagesAsUploaded(
        [FromBody] List<string> imageIds,
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

        var coordinatesToUpdate = await dbContext.Coordinates
            .Where(coordinate => coordinate.UserId == user.Id && imageIds.Contains(coordinate.ImageId!))
            .ToListAsync(cancellationToken);

        foreach (var coordinate in coordinatesToUpdate)
        {
            coordinate.UploadedSuccessfully = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Ok();
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

    private Coordinate CreateCoordinate(
        CoordinateDTO dto,
        int userId,
        string googleSubject,
        Route? route = null)
    {
        var coordinate = new Coordinate
        {
            UserId = userId,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Date = dto.Date,
            Route = route
        };

        if (dto.HasImage)
        {
            coordinate.HasImage = true;
            coordinate.ImageId = Guid.NewGuid().ToString("N");
            coordinate.ImageUrl = imageStorageClient.GetImageUrl(
                googleSubject,
                coordinate.ImageId);
        }

        return coordinate;
    }

    private async Task<CoordinateExportDTO> GetCoordinateExportAsync(
        User user,
        CancellationToken cancellationToken)
    {
        var coordinatesMissingImageUrl = await dbContext.Coordinates
            .Where(coordinate => coordinate.UserId == user.Id
                && coordinate.HasImage
                && coordinate.ImageId != null
                && coordinate.ImageId != string.Empty
                && (coordinate.ImageUrl == null || coordinate.ImageUrl == string.Empty))
            .ToListAsync(cancellationToken);

        foreach (var coordinate in coordinatesMissingImageUrl)
        {
            coordinate.ImageUrl = imageStorageClient.GetImageUrl(
                user.GoogleSubject,
                coordinate.ImageId!);
        }

        if (coordinatesMissingImageUrl.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Backfilled and saved image URLs for {ImageCount} existing coordinates.",
                coordinatesMissingImageUrl.Count);
        }

        var standaloneCoordinates = await dbContext.Coordinates
            .AsNoTracking()
            .Where(coordinate => coordinate.UserId == user.Id && coordinate.RouteId == null)
            .Select(coordinate => new CoordinateDTO
            {
                Latitude = coordinate.Latitude,
                Longitude = coordinate.Longitude,
                Date = coordinate.Date,
                HasImage = coordinate.HasImage,
                ImageId = coordinate.ImageId,
                ImageUrl = coordinate.ImageUrl,
                UploadedSuccessfully = coordinate.UploadedSuccessfully
            })
            .ToListAsync(cancellationToken);

        var routes = await dbContext.Routes
            .AsNoTracking()
            .Where(route => route.UserId == user.Id)
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
                        Date = coordinate.Date,
                        HasImage = coordinate.HasImage,
                        ImageId = coordinate.ImageId,
                        ImageUrl = coordinate.ImageUrl,
                        UploadedSuccessfully = coordinate.UploadedSuccessfully
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        var loadedCoordinates = standaloneCoordinates
            .Concat(routes.SelectMany(route => route.Coordinates))
            .ToList();
        logger.LogInformation(
            "Loaded {CoordinateCount} standalone coordinates and {RouteCount} routes; "
            + "{ImageCount} coordinates have persisted image URLs.",
            standaloneCoordinates.Count,
            routes.Count,
            loadedCoordinates.Count(coordinate => !string.IsNullOrWhiteSpace(coordinate.ImageUrl)));

        return new CoordinateExportDTO
        {
            Coordinates = standaloneCoordinates,
            Routes = routes
        };
    }
}
