using System.Security.Claims;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/lost-reports")]
[Authorize]
public sealed class LostReportsController : ControllerBase
{
    private readonly ILostReportService _service;

    public LostReportsController(ILostReportService service)
    {
        _service = service;
    }

    [HttpPost]
    [ProducesResponseType(typeof(LostReportOwnerDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateLostReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.CreateAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Created($"/api/v1/lost-reports/{result.Value!.Id}", result.Value);
    }

    [HttpGet("mine")]
    [ProducesResponseType(typeof(PagedResponse<LostReportOwnerDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.GetMineAsync(userId, page, pageSize, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(LostReportOwnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var isStaff = User.IsInRole(nameof(UserRole.Staff));
        var result = await _service.GetByIdAsync(id, userId, isStaff, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(LostReportOwnerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateLostReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _service.UpdateAsync(id, userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Close(
        string id,
        [FromQuery] bool permanent = false,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue("sub") ?? string.Empty;
        var isStaff = User.IsInRole(nameof(UserRole.Staff));
        var result = permanent
            ? await _service.DeleteAsync(id, userId, isStaff, cancellationToken)
            : await _service.CloseAsync(id, userId, isStaff, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return NoContent();
    }
}
