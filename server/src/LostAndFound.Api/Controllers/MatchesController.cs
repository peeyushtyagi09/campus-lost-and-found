using System.Security.Claims;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/matches")]
[Authorize]
public sealed class MatchesController : ControllerBase
{
    private readonly IMatchingService _matchingService;

    public MatchesController(IMatchingService matchingService)
    {
        _matchingService = matchingService;
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _matchingService.GetMineAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    [ProducesResponseType(typeof(IReadOnlyList<ItemMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> All(CancellationToken cancellationToken)
    {
        var items = await _matchingService.GetAllAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunOnDemand(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var isStaff = User.IsInRole(nameof(UserRole.Staff));
        var computed = await _matchingService.RecomputeForUserAsync(userId, isStaff, cancellationToken);
        return Ok(new { matchesEvaluated = computed });
    }

    [HttpPost("{id}/confirm")]
    [ProducesResponseType(typeof(ItemMatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Confirm(string id, CancellationToken cancellationToken)
        => UpdateMatchStatusAsync(id, MatchStatus.Confirmed, cancellationToken);

    [HttpPost("{id}/dismiss")]
    [ProducesResponseType(typeof(ItemMatchDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> Dismiss(string id, CancellationToken cancellationToken)
        => UpdateMatchStatusAsync(id, MatchStatus.Dismissed, cancellationToken);

    private async Task<IActionResult> UpdateMatchStatusAsync(
        string id,
        MatchStatus targetStatus,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var isStaff = User.IsInRole(nameof(UserRole.Staff));
        var result = await _matchingService.UpdateStatusAsync(id, userId, isStaff, targetStatus, cancellationToken);
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
