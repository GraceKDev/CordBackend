using CordBackend.CloudFront;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CordBackend.Controllers;

public class ImageController : ControllerBase
{
    [Authorize]
    [HttpGet("api/images/access")]
    [ProducesResponseType<CloudFrontCookies>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult GetImageAccess(
        [FromServices] CloudFrontCookieService cloudFrontCookieService,
        [FromServices] IOptions<CloudFrontOptions> cloudFrontOptions)
    {
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var domain = cloudFrontOptions.Value.DomainName.TrimEnd('/');
        var resource = $"https://{domain}/{Uri.EscapeDataString(userId)}/*";
        var cookies = cloudFrontCookieService.GenerateCookies(resource, TimeSpan.FromHours(1));
        return Ok(cookies);
    }
}
