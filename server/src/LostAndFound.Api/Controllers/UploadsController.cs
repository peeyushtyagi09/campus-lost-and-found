using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Options;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Controllers;

[ApiController]
[Route("api/v1/uploads")]
[Authorize]
public sealed class UploadsController : ControllerBase
{
    private readonly IPhotoSanitizer _sanitizer;
    private readonly IFileStorage _storage;
    private readonly long _maxPhotoBytes;

    public UploadsController(
        IPhotoSanitizer sanitizer,
        IFileStorage storage,
        IOptions<StorageOptions> storageOptions)
    {
        _sanitizer = sanitizer;
        _storage = storage;
        _maxPhotoBytes = storageOptions.Value.MaxPhotoBytes;
    }

    [HttpPost("photos")]
    [EnableRateLimiting(RateLimitOptions.UploadPolicy)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadPhotoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> UploadPhoto(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "A non-empty image file is required.",
                type: "https://tools.ietf.org/html/rfc7807");
        }

        if (file.Length > _maxPhotoBytes)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "Uploaded photo exceeds maximum allowed size.",
                type: "https://tools.ietf.org/html/rfc7807");
        }

        await using var stream = file.OpenReadStream();
        var sanitized = await _sanitizer.SanitizeAsync(stream, cancellationToken);
        if (!sanitized.IsSuccess || sanitized.Value is null)
        {
            return Problem(
                statusCode: sanitized.StatusCode,
                title: sanitized.ErrorTitle,
                type: "https://tools.ietf.org/html/rfc7807");
        }

        var fileName = await _storage.SaveAsync(
            sanitized.Value.Content,
            sanitized.Value.Extension,
            cancellationToken);

        var response = new UploadPhotoResponse(fileName, $"/api/v1/media/{fileName}");
        return Created(response.Url, response);
    }
}
