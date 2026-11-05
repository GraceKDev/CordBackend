using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using CordBackend.Data;
using CordBackend.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CordBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(
    CordDbContext dbContext,
    GoogleAuthSettings googleAuthSettings,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("google")]
    [ProducesResponseType<User>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<User>> Google(
        GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [googleAuthSettings.WebClientId]
                });
        }
        catch (InvalidJwtException exception)
        {
            logger.LogInformation(exception, "Google ID token validation failed.");
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid Google ID token."
            });
        }

        if (string.IsNullOrWhiteSpace(payload.Subject)
            || string.IsNullOrWhiteSpace(payload.Email)
            || !payload.EmailVerified)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Google account is missing required verified identity claims."
            });
        }

        var now = DateTimeOffset.UtcNow;
        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.GoogleSubject == payload.Subject,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                GoogleSubject = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name,
                CreatedAtUtc = now,
                LastLoginAtUtc = now
            };
            dbContext.Users.Add(user);

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is PostgresException
                {
                    SqlState: PostgresErrorCodes.UniqueViolation
                })
            {
                dbContext.Entry(user).State = EntityState.Detached;
                var existingUser = await dbContext.Users.SingleOrDefaultAsync(
                    candidate => candidate.GoogleSubject == payload.Subject,
                    cancellationToken);
                if (existingUser is null)
                {
                    throw;
                }

                user = existingUser;
                user.Email = payload.Email;
                user.DisplayName = payload.Name;
                user.LastLoginAtUtc = now;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        else
        {
            user.Email = payload.Email;
            user.DisplayName = payload.Name;
            user.LastLoginAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(user);
    }
}

public sealed record GoogleLoginRequest(
    [param: Required]
    [param: StringLength(10000, MinimumLength = 1)]
    [property: JsonPropertyName("googleToken")]
    string IdToken);
