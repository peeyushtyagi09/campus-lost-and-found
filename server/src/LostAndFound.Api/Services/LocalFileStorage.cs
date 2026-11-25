using System.Text.RegularExpressions;
using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;

namespace LostAndFound.Api.Services;

public sealed class LocalFileStorage : IFileStorage
{
    private static readonly Regex ValidFileNameRegex = new(
        "^[0-9a-f]{32}\\.(jpg|png|webp)$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly string _rootPath;

    public LocalFileStorage(IOptions<StorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _rootPath = Path.GetFullPath(options.Value.UploadPath);
        Directory.CreateDirectory(_rootPath);
    }

    public bool IsValidFileName(string fileName)
        => !string.IsNullOrWhiteSpace(fileName) && ValidFileNameRegex.IsMatch(fileName);

    public async Task<string> SaveAsync(byte[] content, string extension, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalizedExt = extension.ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}.{normalizedExt}";
        if (!IsValidFileName(fileName))
        {
            throw new ArgumentException("Unsupported file extension.", nameof(extension));
        }

        var fullPath = ResolveSafePath(fileName)!;
        Directory.CreateDirectory(_rootPath);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return fileName;
    }

    public Task<bool> ExistsAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolveSafePath(fileName);
        return Task.FromResult(fullPath is not null && File.Exists(fullPath));
    }

    public Task<StoredMediaFile?> OpenReadAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolveSafePath(fileName);
        if (fullPath is null || !File.Exists(fullPath))
        {
            return Task.FromResult<StoredMediaFile?>(null);
        }

        var contentType = Path.GetExtension(fileName) switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 16 * 1024,
            useAsync: true);

        return Task.FromResult<StoredMediaFile?>(new StoredMediaFile(stream, contentType));
    }

    public Task DeleteAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = ResolveSafePath(fileName);
        if (fullPath is not null && File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string? ResolveSafePath(string fileName)
    {
        if (!IsValidFileName(fileName))
        {
            return null;
        }

        var candidate = Path.GetFullPath(Path.Combine(_rootPath, fileName));
        var rootWithSeparator = _rootPath.EndsWith(Path.DirectorySeparatorChar)
            ? _rootPath
            : _rootPath + Path.DirectorySeparatorChar;

        return candidate.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase)
            ? candidate
            : null;
    }
}
