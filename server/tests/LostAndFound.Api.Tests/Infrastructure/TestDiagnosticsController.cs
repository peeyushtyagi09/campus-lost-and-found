using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Tests.Infrastructure;

public sealed class TestDiagnosticsController : ControllerBase
{
    [HttpGet("/__test/throw")]
    public IActionResult Throw() => throw new InvalidOperationException("internal-only-detail");
}
