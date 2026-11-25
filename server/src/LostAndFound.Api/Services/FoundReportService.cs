using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public sealed class FoundReportService : IFoundReportService
{
    private const int MaxPageSize = 50;
    private readonly IFoundReportRepository _reports;
    private readonly IEncryptionService _encryption;
    private readonly IFileStorage _fileStorage;
    private readonly IMatchingQueue _matchingQueue;
    private readonly ILogger<FoundReportService> _logger;

    public FoundReportService(
        IFoundReportRepository reports,
        IEncryptionService encryption,
        IFileStorage fileStorage,
        IMatchingQueue matchingQueue,
        ILogger<FoundReportService> logger)
    {
        _reports = reports;
        _encryption = encryption;
        _fileStorage = fileStorage;
        _matchingQueue = matchingQueue;
        _logger = logger;
    }

    public async Task<ServiceResult<FoundReportOwnerDto>> CreateAsync(
        string finderUserId,
        CreateFoundReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(finderUserId, out var finderId))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var photoFileName = string.IsNullOrWhiteSpace(request.PhotoFileName) ? null : request.PhotoFileName.Trim();
        if (photoFileName is not null && !await _fileStorage.ExistsAsync(photoFileName, cancellationToken))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(
                StatusCodes.Status400BadRequest,
                "Referenced photo does not exist.");
        }

        var now = DateTime.UtcNow;
        var storageNotes = request.StorageNotes.Trim();
        var finderContact = request.FinderContact.Trim();
        var verificationDetails = request.VerificationDetails.Trim();

        var report = new FoundReport
        {
            Id = ObjectId.GenerateNewId(),
            FinderId = finderId,
            Title = request.Title.Trim(),
            Category = request.Category,
            PublicDescription = request.PublicDescription.Trim(),
            CampusArea = request.CampusArea.Trim(),
            DateFound = request.DateFound.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            HeldAt = request.HeldAt.Trim(),
            PhotoFileName = photoFileName,
            Status = ReportStatus.Open,
            StorageNotes = _encryption.Encrypt(storageNotes),
            FinderContact = _encryption.Encrypt(finderContact),
            VerificationDetails = _encryption.Encrypt(verificationDetails),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _reports.CreateAsync(report, cancellationToken);
        await _matchingQueue.EnqueueFoundAsync(report.Id, cancellationToken);
        _logger.LogInformation("Created found report {ReportId} for user {UserId}.", report.Id, finderId);

        return ServiceResult<FoundReportOwnerDto>.Success(
            ReportMappings.ToFoundOwnerDto(report, storageNotes, finderContact, verificationDetails),
            StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<PagedResponse<FoundReportOwnerDto>>> GetMineAsync(
        string finderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(finderUserId, out var finderId))
        {
            return ServiceResult<PagedResponse<FoundReportOwnerDto>>.Failure(
                StatusCodes.Status401Unauthorized,
                "Authentication required.");
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, totalCount) = await _reports.GetByFinderAsync(
            finderId,
            normalizedPage,
            normalizedSize,
            cancellationToken);

        return ServiceResult<PagedResponse<FoundReportOwnerDto>>.Success(
            new PagedResponse<FoundReportOwnerDto>(items.Select(DecryptToOwnerDto).ToList(), normalizedPage, normalizedSize, totalCount));
    }

    public async Task<ServiceResult<FoundReportOwnerDto>> GetByIdAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null || (report.FinderId != callerId && !isStaff))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        return ServiceResult<FoundReportOwnerDto>.Success(DecryptToOwnerDto(report));
    }

    public async Task<ServiceResult<FoundReportOwnerDto>> UpdateAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        UpdateFoundReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null || (report.FinderId != callerId && !isStaff))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        if (report.Status == ReportStatus.Closed || report.Status == ReportStatus.Returned)
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(
                StatusCodes.Status409Conflict,
                "Closed or returned reports cannot be edited.");
        }

        var photoFileName = string.IsNullOrWhiteSpace(request.PhotoFileName) ? null : request.PhotoFileName.Trim();
        if (photoFileName is not null && !await _fileStorage.ExistsAsync(photoFileName, cancellationToken))
        {
            return ServiceResult<FoundReportOwnerDto>.Failure(
                StatusCodes.Status400BadRequest,
                "Referenced photo does not exist.");
        }

        var storageNotes = request.StorageNotes.Trim();
        var finderContact = request.FinderContact.Trim();
        var verificationDetails = request.VerificationDetails.Trim();

        report.Title = request.Title.Trim();
        report.Category = request.Category;
        report.PublicDescription = request.PublicDescription.Trim();
        report.CampusArea = request.CampusArea.Trim();
        report.DateFound = request.DateFound.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        report.HeldAt = request.HeldAt.Trim();
        report.PhotoFileName = photoFileName;
        report.StorageNotes = _encryption.Encrypt(storageNotes);
        report.FinderContact = _encryption.Encrypt(finderContact);
        report.VerificationDetails = _encryption.Encrypt(verificationDetails);
        report.UpdatedAt = DateTime.UtcNow;

        await _reports.UpdateAsync(report, cancellationToken);
        return ServiceResult<FoundReportOwnerDto>.Success(
            ReportMappings.ToFoundOwnerDto(report, storageNotes, finderContact, verificationDetails));
    }

    public async Task<ServiceResult<bool>> CloseAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<bool>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null || (report.FinderId != callerId && !isStaff))
        {
            return ServiceResult<bool>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        if (report.Status != ReportStatus.Closed)
        {
            report.Status = ReportStatus.Closed;
            report.UpdatedAt = DateTime.UtcNow;
            await _reports.UpdateAsync(report, cancellationToken);
        }

        return ServiceResult<bool>.Success(true, StatusCodes.Status204NoContent);
    }

    private FoundReportOwnerDto DecryptToOwnerDto(FoundReport report)
    {
        return ReportMappings.ToFoundOwnerDto(
            report,
            storageNotes: _encryption.Decrypt(report.StorageNotes),
            finderContact: _encryption.Decrypt(report.FinderContact),
            verificationDetails: _encryption.Decrypt(report.VerificationDetails));
    }
}
