using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public sealed class LostReportService : ILostReportService
{
    private const int MaxPageSize = 50;
    private readonly ILostReportRepository _reports;
    private readonly IEncryptionService _encryption;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<LostReportService> _logger;

    public LostReportService(
        ILostReportRepository reports,
        IEncryptionService encryption,
        IFileStorage fileStorage,
        ILogger<LostReportService> logger)
    {
        _reports = reports;
        _encryption = encryption;
        _fileStorage = fileStorage;
        _logger = logger;
    }

    public async Task<ServiceResult<LostReportOwnerDto>> CreateAsync(
        string reporterUserId,
        CreateLostReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reporterUserId, out var reporterId))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var photoFileName = string.IsNullOrWhiteSpace(request.PhotoFileName) ? null : request.PhotoFileName.Trim();
        if (photoFileName is not null && !await _fileStorage.ExistsAsync(photoFileName, cancellationToken))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(
                StatusCodes.Status400BadRequest,
                "Referenced photo does not exist.");
        }

        var now = DateTime.UtcNow;
        var privateDetails = request.PrivateDetails.Trim();
        var contactPhone = request.ContactPhone.Trim();
        var contactEmail = request.ContactEmail.Trim().ToLowerInvariant();

        var report = new LostReport
        {
            Id = ObjectId.GenerateNewId(),
            ReporterId = reporterId,
            Title = request.Title.Trim(),
            Category = request.Category,
            PublicDescription = request.PublicDescription.Trim(),
            CampusArea = request.CampusArea.Trim(),
            DateLost = request.DateLost.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            PhotoFileName = photoFileName,
            Status = ReportStatus.Open,
            PrivateDetails = _encryption.Encrypt(privateDetails),
            ContactPhone = _encryption.Encrypt(contactPhone),
            ContactEmail = _encryption.Encrypt(contactEmail),
            CreatedAt = now,
            UpdatedAt = now
        };

        await _reports.CreateAsync(report, cancellationToken);
        _logger.LogInformation("Created lost report {ReportId} for user {UserId}.", report.Id, reporterId);

        var dto = ReportMappings.ToOwnerDto(report, privateDetails, contactPhone, contactEmail);
        return ServiceResult<LostReportOwnerDto>.Success(dto, StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<PagedResponse<LostReportOwnerDto>>> GetMineAsync(
        string reporterUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reporterUserId, out var reporterId))
        {
            return ServiceResult<PagedResponse<LostReportOwnerDto>>.Failure(
                StatusCodes.Status401Unauthorized,
                "Authentication required.");
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, totalCount) = await _reports.GetByReporterAsync(
            reporterId,
            normalizedPage,
            normalizedSize,
            cancellationToken);

        var dtos = items.Select(DecryptToOwnerDto).ToList();
        return ServiceResult<PagedResponse<LostReportOwnerDto>>.Success(
            new PagedResponse<LostReportOwnerDto>(dtos, normalizedPage, normalizedSize, totalCount));
    }

    public async Task<ServiceResult<LostReportOwnerDto>> GetByIdAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null || (report.ReporterId != callerId && !isStaff))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        return ServiceResult<LostReportOwnerDto>.Success(DecryptToOwnerDto(report));
    }

    public async Task<ServiceResult<LostReportOwnerDto>> UpdateAsync(
        string reportId,
        string callerUserId,
        UpdateLostReportRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        var report = await _reports.GetByIdAsync(id, cancellationToken);
        if (report is null || report.ReporterId != callerId)
        {
            return ServiceResult<LostReportOwnerDto>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        if (report.Status == ReportStatus.Closed || report.Status == ReportStatus.Returned)
        {
            return ServiceResult<LostReportOwnerDto>.Failure(
                StatusCodes.Status409Conflict,
                "Closed or returned reports cannot be edited.");
        }

        var photoFileName = string.IsNullOrWhiteSpace(request.PhotoFileName) ? null : request.PhotoFileName.Trim();
        if (photoFileName is not null && !await _fileStorage.ExistsAsync(photoFileName, cancellationToken))
        {
            return ServiceResult<LostReportOwnerDto>.Failure(
                StatusCodes.Status400BadRequest,
                "Referenced photo does not exist.");
        }

        var privateDetails = request.PrivateDetails.Trim();
        var contactPhone = request.ContactPhone.Trim();
        var contactEmail = request.ContactEmail.Trim().ToLowerInvariant();

        report.Title = request.Title.Trim();
        report.Category = request.Category;
        report.PublicDescription = request.PublicDescription.Trim();
        report.CampusArea = request.CampusArea.Trim();
        report.DateLost = request.DateLost.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        report.PhotoFileName = photoFileName;
        report.PrivateDetails = _encryption.Encrypt(privateDetails);
        report.ContactPhone = _encryption.Encrypt(contactPhone);
        report.ContactEmail = _encryption.Encrypt(contactEmail);
        report.UpdatedAt = DateTime.UtcNow;

        await _reports.UpdateAsync(report, cancellationToken);
        _logger.LogInformation("Updated lost report {ReportId}.", report.Id);

        return ServiceResult<LostReportOwnerDto>.Success(
            ReportMappings.ToOwnerDto(report, privateDetails, contactPhone, contactEmail));
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
        if (report is null || (report.ReporterId != callerId && !isStaff))
        {
            return ServiceResult<bool>.Failure(StatusCodes.Status404NotFound, "Report not found.");
        }

        if (report.Status != ReportStatus.Closed)
        {
            report.Status = ReportStatus.Closed;
            report.UpdatedAt = DateTime.UtcNow;
            await _reports.UpdateAsync(report, cancellationToken);
            _logger.LogInformation("Closed lost report {ReportId}.", report.Id);
        }

        return ServiceResult<bool>.Success(true, StatusCodes.Status204NoContent);
    }

    private LostReportOwnerDto DecryptToOwnerDto(LostReport report)
    {
        return ReportMappings.ToOwnerDto(
            report,
            privateDetails: _encryption.Decrypt(report.PrivateDetails),
            contactPhone: _encryption.Decrypt(report.ContactPhone),
            contactEmail: _encryption.Decrypt(report.ContactEmail));
    }
}
