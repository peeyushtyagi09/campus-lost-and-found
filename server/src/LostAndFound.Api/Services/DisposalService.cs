using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface IDisposalService
{
    Task<long> FlagExpiredFoundItemsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<PublicReportDto>> GetEligibleAsync(CancellationToken cancellationToken);

    Task<ServiceResult<DisposalDto>> ApproveDisposalAsync(
        string foundReportId,
        string staffUserId,
        ApproveDisposalRequest request,
        CancellationToken cancellationToken);
}

public sealed class DisposalService : IDisposalService
{
    private readonly IFoundReportRepository _foundReports;
    private readonly IDisposalRepository _disposals;
    private readonly ICustodyService _custody;
    private readonly IFileStorage _fileStorage;
    private readonly IEncryptionService _encryption;
    private readonly IAuditService _audit;
    private readonly int _retentionDays;

    public DisposalService(
        IFoundReportRepository foundReports,
        IDisposalRepository disposals,
        ICustodyService custody,
        IFileStorage fileStorage,
        IEncryptionService encryption,
        IAuditService audit,
        IOptions<RetentionOptions> retentionOptions)
    {
        _foundReports = foundReports;
        _disposals = disposals;
        _custody = custody;
        _fileStorage = fileStorage;
        _encryption = encryption;
        _audit = audit;
        _retentionDays = retentionOptions.Value.DisposalDays;
    }

    public Task<long> FlagExpiredFoundItemsAsync(CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);
        return _foundReports.FlagEligibleForDisposalAsync(cutoff, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicReportDto>> GetEligibleAsync(CancellationToken cancellationToken)
    {
        var items = await _foundReports.GetEligibleForDisposalAsync(cancellationToken);
        return items.Select(ReportMappings.ToPublicDto).ToList();
    }

    public async Task<ServiceResult<DisposalDto>> ApproveDisposalAsync(
        string foundReportId,
        string staffUserId,
        ApproveDisposalRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(foundReportId, out var foundId) || !ObjectId.TryParse(staffUserId, out var staffId))
        {
            return ServiceResult<DisposalDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        var found = await _foundReports.GetByIdAsync(foundId, cancellationToken);
        if (found is null)
        {
            return ServiceResult<DisposalDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        if (found.Status is ReportStatus.Returned or ReportStatus.Closed)
        {
            return ServiceResult<DisposalDto>.Failure(
                StatusCodes.Status409Conflict,
                "Item has already been returned or closed.");
        }

        var existing = await _disposals.GetByFoundReportIdAsync(foundId, cancellationToken);
        if (existing is not null)
        {
            return ServiceResult<DisposalDto>.Failure(
                StatusCodes.Status409Conflict,
                "Item has already been disposed.");
        }

        var now = DateTime.UtcNow;
        var disposal = new Disposal
        {
            Id = ObjectId.GenerateNewId(),
            FoundReportId = foundId,
            Method = request.Method,
            ApprovedBy = staffId,
            Notes = _encryption.Encrypt(request.Notes.Trim()),
            DisposedAt = now
        };
        await _disposals.CreateAsync(disposal, cancellationToken);

        var photoToDelete = found.PhotoFileName;
        found.Status = ReportStatus.Closed;
        found.EligibleForDisposal = false;
        found.PhotoFileName = null;
        found.UpdatedAt = now;
        await _foundReports.UpdateAsync(found, cancellationToken);

        if (!string.IsNullOrWhiteSpace(photoToDelete))
        {
            await _fileStorage.DeleteAsync(photoToDelete, cancellationToken);
        }

        await _custody.RecordEventAsync(
            foundId,
            CustodyEventType.Disposed,
            staffId,
            UserRole.Staff,
            $"Disposed via {request.Method}.",
            cancellationToken);

        await _audit.RecordAsync(
            "ItemDisposed",
            staffId,
            UserRole.Staff,
            foundId.ToString(),
            $"Approved disposal via {request.Method}.",
            cancellationToken);

        return ServiceResult<DisposalDto>.Success(
            new DisposalDto(disposal.Id.ToString(), foundId.ToString(), disposal.Method, disposal.DisposedAt),
            StatusCodes.Status201Created);
    }
}
