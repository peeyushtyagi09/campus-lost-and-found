using System.Security.Claims;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/handovers")]
[Authorize(Policy = AuthPolicies.RequireStaff)]
public sealed class HandoversController : ControllerBase
{
    private readonly IClaimHandoverService _service;

    public HandoversController(IClaimHandoverService service)
    {
        _service = service;
    }

    [HttpPost("{claimId}/complete")]
    [ProducesResponseType(typeof(HandoverDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(
        string claimId,
        [FromBody] CompleteHandoverRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.CompleteHandoverAsync(claimId, staffId, request, cancellationToken);
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
