namespace LostAndFound.Api.Services;

public sealed record SanitizedPhoto(byte[] Content, string Extension, string ContentType);

public interface IPhotoSanitizer
{
    Task<ServiceResult<SanitizedPhoto>> SanitizeAsync(Stream inputStream, CancellationToken cancellationToken);
}
