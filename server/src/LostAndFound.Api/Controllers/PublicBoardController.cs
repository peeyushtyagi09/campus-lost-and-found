using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/public")]
public sealed class PublicBoardController : ControllerBase
{
    private readonly IPublicBoardService _boardService;

    public PublicBoardController(IPublicBoardService boardService)
    {
        _boardService = boardService;
    }

    [HttpGet("board")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<PublicReportDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBoard(
        [FromQuery] ItemCategory? category = null,
        [FromQuery] string? area = null,
        [FromQuery] string? q = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _boardService.GetBoardAsync(category, area, q, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
