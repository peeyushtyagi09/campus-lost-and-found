using System.Security.Claims;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/custody")]
[Authorize]
public sealed class CustodyController : ControllerBase
{
    private readonly ICustodyService _custodyService;

    public CustodyController(ICustodyService custodyService)
    {
        _custodyService = custodyService;
    }

    [HttpGet("{reportId}")]
    [ProducesResponseType(typeof(CustodyTimelineDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeline(string reportId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var isStaff = User.IsInRole(nameof(UserRole.Staff));
        var result = await _custodyService.GetTimelineAsync(reportId, userId, isStaff, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }
}
