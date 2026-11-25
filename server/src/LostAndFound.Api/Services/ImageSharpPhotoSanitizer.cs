using LostAndFound.Api.Options;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace LostAndFound.Api.Services;

/// <summary>
/// Decodes uploaded images, verifies bounds/format, auto-orients, strips all metadata, and re-encodes.
/// </summary>
public sealed class ImageSharpPhotoSanitizer : IPhotoSanitizer
{
    public const int MaxSourceDimension = 8000;
    public const int MaxStoredDimension = 1600;
    private readonly long _maxPhotoBytes;

    public ImageSharpPhotoSanitizer(IOptions<StorageOptions> storageOptions)
    {
        ArgumentNullException.ThrowIfNull(storageOptions);
        _maxPhotoBytes = storageOptions.Value.MaxPhotoBytes;
    }

    public async Task<ServiceResult<SanitizedPhoto>> SanitizeAsync(
        Stream inputStream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inputStream);

        await using var buffered = new MemoryStream();
        await inputStream.CopyToAsync(buffered, cancellationToken);

        if (buffered.Length == 0)
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status400BadRequest,
                "Uploaded photo file is empty.");
        }

        if (buffered.Length > _maxPhotoBytes)
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status413PayloadTooLarge,
                "Uploaded photo exceeds maximum allowed size.");
        }

        buffered.Position = 0;

        ImageInfo? info;
        IImageFormat? format;
        try
        {
            info = await Image.IdentifyAsync(buffered, cancellationToken);
            buffered.Position = 0;
            format = await Image.DetectFormatAsync(buffered, cancellationToken);
        }
        catch (UnknownImageFormatException)
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status400BadRequest,
                "Unsupported or invalid image file.");
        }
        catch (InvalidImageContentException)
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status400BadRequest,
                "Unsupported or invalid image file.");
        }

        if (info is null || format is null || !IsAllowedFormat(format))
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status400BadRequest,
                "Only JPEG, PNG, and WebP images are allowed.");
        }

        if (info.Width <= 0 || info.Height <= 0 || info.Width > MaxSourceDimension || info.Height > MaxSourceDimension)
        {
            return ServiceResult<SanitizedPhoto>.Failure(
                StatusCodes.Status400BadRequest,
                "Image dimensions exceed allowed limits.");
        }

        buffered.Position = 0;
        using var image = await Image.LoadAsync(buffered, cancellationToken);

        image.Mutate(context =>
        {
            context.AutoOrient();
            if (image.Width > MaxStoredDimension || image.Height > MaxStoredDimension)
            {
                context.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxStoredDimension, MaxStoredDimension)
                });
            }
        });

        StripAllMetadata(image);

        await using var output = new MemoryStream();
        string extension;
        string contentType;

        if (format is PngFormat)
        {
            await image.SaveAsPngAsync(output, new PngEncoder(), cancellationToken);
            extension = "png";
            contentType = "image/png";
        }
        else if (format is WebpFormat)
        {
            await image.SaveAsWebpAsync(output, new WebpEncoder(), cancellationToken);
            extension = "webp";
            contentType = "image/webp";
        }
        else
        {
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 }, cancellationToken);
            extension = "jpg";
            contentType = "image/jpeg";
        }

        return ServiceResult<SanitizedPhoto>.Success(new SanitizedPhoto(output.ToArray(), extension, contentType));
    }

    private static bool IsAllowedFormat(IImageFormat format)
        => format is JpegFormat or PngFormat or WebpFormat;

    private static void StripAllMetadata(Image image)
    {
        image.Metadata.ExifProfile = null;
        image.Metadata.IptcProfile = null;
        image.Metadata.XmpProfile = null;
        image.Metadata.IccProfile = null;
        image.Metadata.CicpProfile = null;

        foreach (var frame in image.Frames)
        {
            frame.Metadata.ExifProfile = null;
            frame.Metadata.IptcProfile = null;
            frame.Metadata.XmpProfile = null;
            frame.Metadata.IccProfile = null;
            frame.Metadata.CicpProfile = null;
        }
    }
}
