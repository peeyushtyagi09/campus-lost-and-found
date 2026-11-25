using System.Security.Claims;
using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Policy = AuthPolicies.RequireAdmin)]
public sealed class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IAuditService _auditService;

    public AdminController(IAdminService adminService, IAuditService auditService)
    {
        _adminService = adminService;
        _auditService = auditService;
    }

    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _adminService.ListUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPost("staff")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateStaff(
        [FromBody] CreateStaffRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _adminService.CreateStaffAsync(adminId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Created($"/api/v1/admin/users", result.Value);
    }

    [HttpPatch("users/{id}/role")]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(
        string id,
        [FromBody] UpdateUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue("sub") ?? string.Empty;
        var result = await _adminService.UpdateRoleAsync(adminId, id, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(
                statusCode: result.StatusCode,
                title: result.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        return Ok(result.Value);
    }

    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(PagedResponse<AuditEventDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var logs = await _auditService.ListAsync(page, pageSize, cancellationToken);
        return Ok(logs);
    }
}
