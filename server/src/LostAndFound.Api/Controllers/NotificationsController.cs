using System.Security.Claims;
using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _service;

    public NotificationsController(INotificationService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.GetMineAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpPost("{id}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.MarkReadAsync(id, userId, cancellationToken);
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
