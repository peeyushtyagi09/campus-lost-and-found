using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController : ControllerBase
{
    private readonly IFileStorage _storage;

    public MediaController(IFileStorage storage)
    {
        _storage = storage;
    }

    [HttpGet("{fileName}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMedia(string fileName, CancellationToken cancellationToken)
    {
        if (!_storage.IsValidFileName(fileName))
        {
            return NotFound();
        }

        var file = await _storage.OpenReadAsync(fileName, cancellationToken);
        if (file is null)
        {
            return NotFound();
        }

        Response.Headers[HeaderNames.CacheControl] = "public, max-age=31536000, immutable";
        return File(file.Stream, file.ContentType);
    }
}
