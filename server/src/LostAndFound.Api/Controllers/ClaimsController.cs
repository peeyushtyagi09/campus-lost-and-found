using System.Security.Claims;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/claims")]
[Authorize]
public sealed class ClaimsController : ControllerBase
{
    private readonly IClaimHandoverService _service;

    public ClaimsController(IClaimHandoverService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Submit(
        [FromBody] CreateClaimRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var roleClaim = User.FindFirstValue("role");
        var role = Enum.TryParse<UserRole>(roleClaim, out var parsedRole) ? parsedRole : UserRole.User;
        var result = await _service.SubmitClaimAsync(userId, role, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Created($"/api/v1/claims/{result.Value!.Id}", result.Value);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(IReadOnlyList<ClaimDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.GetMyClaimsAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpPost("{id}/withdraw")]
    [ProducesResponseType(typeof(ClaimDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Withdraw(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.WithdrawClaimAsync(id, userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpGet("review")]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    [ProducesResponseType(typeof(IReadOnlyList<StaffClaimReviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewQueue(
        [FromQuery] ClaimStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _service.GetReviewQueueAsync(status, cancellationToken);
        return Ok(items);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    [ProducesResponseType(typeof(StaffClaimReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        string id,
        [FromBody] ReviewClaimRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.ApproveClaimAsync(id, staffId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpPost("{id}/reject")]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    [ProducesResponseType(typeof(StaffClaimReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        string id,
        [FromBody] ReviewClaimRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.RejectClaimAsync(id, staffId, request, cancellationToken);
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
