namespace LostAndFound.Api.Services;

public sealed record StoredMediaFile(Stream Stream, string ContentType);

public interface IFileStorage
{
    bool IsValidFileName(string fileName);

    Task<string> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken);

    Task<StoredMediaFile?> OpenReadAsync(string fileName, CancellationToken cancellationToken);

    Task DeleteAsync(string fileName, CancellationToken cancellationToken);
}
