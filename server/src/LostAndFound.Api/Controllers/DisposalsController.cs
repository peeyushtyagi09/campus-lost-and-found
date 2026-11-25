using System.Security.Claims;
using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/disposals")]
[Authorize(Policy = AuthPolicies.RequireStaff)]
public sealed class DisposalsController : ControllerBase
{
    private readonly IDisposalService _disposalService;

    public DisposalsController(IDisposalService disposalService)
    {
        _disposalService = disposalService;
    }

    [HttpGet("eligible")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Eligible(CancellationToken cancellationToken)
    {
        var items = await _disposalService.GetEligibleAsync(cancellationToken);
        return Ok(items);
    }

    [HttpPost("{foundReportId}/approve")]
    [ProducesResponseType(typeof(DisposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(
        string foundReportId,
        [FromBody] ApproveDisposalRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _disposalService.ApproveDisposalAsync(foundReportId, staffId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Created($"/api/v1/custody/{result.Value!.FoundReportId}", result.Value);
    }
}
