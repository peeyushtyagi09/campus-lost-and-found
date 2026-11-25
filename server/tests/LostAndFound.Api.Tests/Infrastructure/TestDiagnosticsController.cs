using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Tests.Infrastructure;

public sealed class TestDiagnosticsController : ControllerBase
{
    [HttpGet("/__test/throw")]
    public IActionResult Throw() => throw new InvalidOperationException("internal-only-detail");

    [HttpGet("/__test/staff")]
    [Authorize(Policy = AuthPolicies.RequireStaff)]
    public IActionResult StaffOnly() => Ok(new { ok = true });

    [HttpGet("/__test/admin")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    public IActionResult AdminOnly() => Ok(new { ok = true });
}
