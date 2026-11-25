using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Dtos.Reports;

public sealed record UploadPhotoResponse(string FileName, string Url);

public sealed record PublicReportDto(
    string Id,
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateLost,
    string? PhotoUrl,
    ReportStatus Status,
    DateTime PostedAt);

public sealed record LostReportOwnerDto(
    string Id,
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateLost,
    string? PhotoFileName,
    string? PhotoUrl,
    ReportStatus Status,
    string PrivateDetails,
    string ContactPhone,
    string ContactEmail,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateLostReportRequest(
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateLost,
    string? PhotoFileName,
    string PrivateDetails,
    string ContactPhone,
    string ContactEmail);

public sealed record UpdateLostReportRequest(
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateLost,
    string? PhotoFileName,
    string PrivateDetails,
    string ContactPhone,
    string ContactEmail);

public static class ReportMappings
{
    public static string? BuildPhotoUrl(string? photoFileName)
        => string.IsNullOrWhiteSpace(photoFileName) ? null : $"/api/v1/media/{photoFileName}";

    /// <summary>
    /// Explicit allow-list mapping from Public fields only.
    /// </summary>
    public static PublicReportDto ToPublicDto(LostReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new PublicReportDto(
            Id: report.Id.ToString(),
            Title: report.Title,
            Category: report.Category,
            PublicDescription: report.PublicDescription,
            CampusArea: report.CampusArea,
            DateLost: DateOnly.FromDateTime(report.DateLost),
            PhotoUrl: BuildPhotoUrl(report.PhotoFileName),
            Status: report.Status,
            PostedAt: report.CreatedAt);
    }

    public static LostReportOwnerDto ToOwnerDto(
        LostReport report,
        string privateDetails,
        string contactPhone,
        string contactEmail)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new LostReportOwnerDto(
            Id: report.Id.ToString(),
            Title: report.Title,
            Category: report.Category,
            PublicDescription: report.PublicDescription,
            CampusArea: report.CampusArea,
            DateLost: DateOnly.FromDateTime(report.DateLost),
            PhotoFileName: report.PhotoFileName,
            PhotoUrl: BuildPhotoUrl(report.PhotoFileName),
            Status: report.Status,
            PrivateDetails: privateDetails,
            ContactPhone: contactPhone,
            ContactEmail: contactEmail,
            CreatedAt: report.CreatedAt,
            UpdatedAt: report.UpdatedAt);
    }
}
