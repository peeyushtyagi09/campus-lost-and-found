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
    DateTime PostedAt,
    string? HeldAt = null);

public sealed record FoundReportOwnerDto(
    string Id,
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateFound,
    string HeldAt,
    string? PhotoFileName,
    string? PhotoUrl,
    ReportStatus Status,
    string StorageNotes,
    string FinderContact,
    string VerificationDetails,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateFoundReportRequest(
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateFound,
    string HeldAt,
    string? PhotoFileName,
    string StorageNotes,
    string FinderContact,
    string VerificationDetails);

public sealed record UpdateFoundReportRequest(
    string Title,
    ItemCategory Category,
    string PublicDescription,
    string CampusArea,
    DateOnly DateFound,
    string HeldAt,
    string? PhotoFileName,
    string StorageNotes,
    string FinderContact,
    string VerificationDetails);

public sealed record ItemMatchDto(
    string Id,
    string LostReportId,
    string FoundReportId,
    int Score,
    string Explanation,
    MatchStatus Status,
    DateTime CreatedAt,
    PublicReportDto FoundItem);

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
            PostedAt: report.CreatedAt,
            HeldAt: null);
    }

    public static PublicReportDto ToPublicDto(FoundReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new PublicReportDto(
            Id: report.Id.ToString(),
            Title: report.Title,
            Category: report.Category,
            PublicDescription: report.PublicDescription,
            CampusArea: report.CampusArea,
            DateLost: DateOnly.FromDateTime(report.DateFound),
            PhotoUrl: BuildPhotoUrl(report.PhotoFileName),
            Status: report.Status,
            PostedAt: report.CreatedAt,
            HeldAt: report.HeldAt);
    }

    public static FoundReportOwnerDto ToFoundOwnerDto(
        FoundReport report,
        string storageNotes,
        string finderContact,
        string verificationDetails)
    {
        ArgumentNullException.ThrowIfNull(report);
        return new FoundReportOwnerDto(
            Id: report.Id.ToString(),
            Title: report.Title,
            Category: report.Category,
            PublicDescription: report.PublicDescription,
            CampusArea: report.CampusArea,
            DateFound: DateOnly.FromDateTime(report.DateFound),
            HeldAt: report.HeldAt,
            PhotoFileName: report.PhotoFileName,
            PhotoUrl: BuildPhotoUrl(report.PhotoFileName),
            Status: report.Status,
            StorageNotes: storageNotes,
            FinderContact: finderContact,
            VerificationDetails: verificationDetails,
            CreatedAt: report.CreatedAt,
            UpdatedAt: report.UpdatedAt);
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
