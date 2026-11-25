using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface IClaimHandoverService
{
    Task<ServiceResult<ClaimDto>> SubmitClaimAsync(
        string claimantUserId,
        UserRole callerRole,
        CreateClaimRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<ClaimDto>>> GetMyClaimsAsync(
        string claimantUserId,
        CancellationToken cancellationToken);

    Task<ServiceResult<ClaimDto>> WithdrawClaimAsync(
        string claimId,
        string callerUserId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<StaffClaimReviewDto>> GetReviewQueueAsync(
        ClaimStatus? status,
        CancellationToken cancellationToken);

    Task<ServiceResult<StaffClaimReviewDto>> ApproveClaimAsync(
        string claimId,
        string staffUserId,
        ReviewClaimRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<StaffClaimReviewDto>> RejectClaimAsync(
        string claimId,
        string staffUserId,
        ReviewClaimRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<HandoverDto>> CompleteHandoverAsync(
        string claimId,
        string staffUserId,
        CompleteHandoverRequest request,
        CancellationToken cancellationToken);
}

public sealed class ClaimHandoverService : IClaimHandoverService
{
    private readonly IClaimRepository _claims;
    private readonly IFoundReportRepository _foundReports;
    private readonly ILostReportRepository _lostReports;
    private readonly IHandoverRepository _handovers;
    private readonly ICustodyService _custody;
    private readonly IEncryptionService _encryption;
    private readonly ILogger<ClaimHandoverService> _logger;

    public ClaimHandoverService(
        IClaimRepository claims,
        IFoundReportRepository foundReports,
        ILostReportRepository lostReports,
        IHandoverRepository handovers,
        ICustodyService custody,
        IEncryptionService encryption,
        ILogger<ClaimHandoverService> logger)
    {
        _claims = claims;
        _foundReports = foundReports;
        _lostReports = lostReports;
        _handovers = handovers;
        _custody = custody;
        _encryption = encryption;
        _logger = logger;
    }

    public async Task<ServiceResult<ClaimDto>> SubmitClaimAsync(
        string claimantUserId,
        UserRole callerRole,
        CreateClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(claimantUserId, out var claimantId))
        {
            return ServiceResult<ClaimDto>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        if (!ObjectId.TryParse(request.FoundReportId, out var foundId))
        {
            return ServiceResult<ClaimDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        var found = await _foundReports.GetByIdAsync(foundId, cancellationToken);
        if (found is null)
        {
            return ServiceResult<ClaimDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        if (found.Status is not (ReportStatus.Open or ReportStatus.Matched))
        {
            return ServiceResult<ClaimDto>.Failure(
                StatusCodes.Status409Conflict,
                "Found item is not available for new claims.");
        }

        ObjectId? lostId = null;
        if (!string.IsNullOrWhiteSpace(request.LostReportId))
        {
            if (!ObjectId.TryParse(request.LostReportId, out var parsedLostId))
            {
                return ServiceResult<ClaimDto>.Failure(StatusCodes.Status400BadRequest, "Invalid lost report id.");
            }

            var lost = await _lostReports.GetByIdAsync(parsedLostId, cancellationToken);
            if (lost is null || lost.ReporterId != claimantId)
            {
                return ServiceResult<ClaimDto>.Failure(StatusCodes.Status404NotFound, "Lost report not found.");
            }

            lostId = parsedLostId;
        }

        var existingForItem = await _claims.GetByFoundReportAsync(foundId, cancellationToken);
        if (existingForItem.Any(c => c.ClaimantId == claimantId && c.Status is ClaimStatus.Pending or ClaimStatus.Approved))
        {
            return ServiceResult<ClaimDto>.Failure(
                StatusCodes.Status409Conflict,
                "An active claim for this item already exists.");
        }

        var now = DateTime.UtcNow;
        var answers = request.VerificationAnswers.Trim();
        var claim = new Claim
        {
            Id = ObjectId.GenerateNewId(),
            ClaimantId = claimantId,
            FoundReportId = foundId,
            LostReportId = lostId,
            VerificationAnswers = _encryption.Encrypt(answers),
            Status = ClaimStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _claims.CreateAsync(claim, cancellationToken);
        await _custody.RecordEventAsync(
            foundId,
            CustodyEventType.ClaimSubmitted,
            claimantId,
            callerRole,
            "Ownership claim submitted.",
            cancellationToken);

        _logger.LogInformation("Claim {ClaimId} submitted for found report {FoundReportId}.", claim.Id, foundId);
        return ServiceResult<ClaimDto>.Success(
            ToClaimDto(claim, answers, null, ReportMappings.ToPublicDto(found)),
            StatusCodes.Status201Created);
    }

    public async Task<ServiceResult<IReadOnlyList<ClaimDto>>> GetMyClaimsAsync(
        string claimantUserId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(claimantUserId, out var claimantId))
        {
            return ServiceResult<IReadOnlyList<ClaimDto>>.Failure(StatusCodes.Status401Unauthorized, "Authentication required.");
        }

        var claims = await _claims.GetByClaimantAsync(claimantId, cancellationToken);
        var foundIds = claims.Select(c => c.FoundReportId).ToList();
        var foundMap = (await _foundReports.GetByIdsAsync(foundIds, cancellationToken))
            .ToDictionary(f => f.Id);

        var dtos = claims.Select(c =>
        {
            foundMap.TryGetValue(c.FoundReportId, out var found);
            return ToClaimDto(
                c,
                _encryption.Decrypt(c.VerificationAnswers),
                c.ReviewNotes is null ? null : _encryption.Decrypt(c.ReviewNotes),
                found is null ? null : ReportMappings.ToPublicDto(found));
        }).ToList();

        return ServiceResult<IReadOnlyList<ClaimDto>>.Success(dtos);
    }

    public async Task<ServiceResult<ClaimDto>> WithdrawClaimAsync(
        string claimId,
        string callerUserId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(claimId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<ClaimDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        if (claim is null || claim.ClaimantId != callerId)
        {
            return ServiceResult<ClaimDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        if (claim.Status != ClaimStatus.Pending)
        {
            return ServiceResult<ClaimDto>.Failure(
                StatusCodes.Status409Conflict,
                "Only pending claims can be withdrawn.");
        }

        claim.Status = ClaimStatus.Withdrawn;
        claim.UpdatedAt = DateTime.UtcNow;
        await _claims.UpdateAsync(claim, cancellationToken);

        var found = await _foundReports.GetByIdAsync(claim.FoundReportId, cancellationToken);
        return ServiceResult<ClaimDto>.Success(ToClaimDto(
            claim,
            _encryption.Decrypt(claim.VerificationAnswers),
            claim.ReviewNotes is null ? null : _encryption.Decrypt(claim.ReviewNotes),
            found is null ? null : ReportMappings.ToPublicDto(found)));
    }

    public async Task<IReadOnlyList<StaffClaimReviewDto>> GetReviewQueueAsync(
        ClaimStatus? status,
        CancellationToken cancellationToken)
    {
        var claims = await _claims.GetForReviewQueueAsync(status, cancellationToken);
        var foundIds = claims.Select(c => c.FoundReportId).ToList();
        var foundMap = (await _foundReports.GetByIdsAsync(foundIds, cancellationToken))
            .ToDictionary(f => f.Id);

        return claims.Select(c =>
        {
            foundMap.TryGetValue(c.FoundReportId, out var found);
            return ToStaffReviewDto(c, found);
        }).ToList();
    }

    public Task<ServiceResult<StaffClaimReviewDto>> ApproveClaimAsync(
        string claimId,
        string staffUserId,
        ReviewClaimRequest request,
        CancellationToken cancellationToken)
        => ReviewClaimAsync(claimId, staffUserId, approve: true, request, cancellationToken);

    public Task<ServiceResult<StaffClaimReviewDto>> RejectClaimAsync(
        string claimId,
        string staffUserId,
        ReviewClaimRequest request,
        CancellationToken cancellationToken)
        => ReviewClaimAsync(claimId, staffUserId, approve: false, request, cancellationToken);

    public async Task<ServiceResult<HandoverDto>> CompleteHandoverAsync(
        string claimId,
        string staffUserId,
        CompleteHandoverRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(claimId, out var id) || !ObjectId.TryParse(staffUserId, out var staffId))
        {
            return ServiceResult<HandoverDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        if (claim is null)
        {
            return ServiceResult<HandoverDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        if (claim.Status != ClaimStatus.Approved)
        {
            return ServiceResult<HandoverDto>.Failure(
                StatusCodes.Status409Conflict,
                "Handover requires an approved claim.");
        }

        var existingHandover = await _handovers.GetByClaimIdAsync(claim.Id, cancellationToken);
        if (existingHandover is not null)
        {
            return ServiceResult<HandoverDto>.Failure(
                StatusCodes.Status409Conflict,
                "Handover has already been completed for this claim.");
        }

        var found = await _foundReports.GetByIdAsync(claim.FoundReportId, cancellationToken);
        if (found is null)
        {
            return ServiceResult<HandoverDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        if (found.Status == ReportStatus.Returned || found.Status == ReportStatus.Closed)
        {
            return ServiceResult<HandoverDto>.Failure(
                StatusCodes.Status409Conflict,
                "Found item has already been returned or closed.");
        }

        var now = DateTime.UtcNow;
        var notes = string.IsNullOrWhiteSpace(request.Notes) ? "ID checked and witness confirmed." : request.Notes.Trim();
        var handover = new Handover
        {
            Id = ObjectId.GenerateNewId(),
            ClaimId = claim.Id,
            FoundReportId = found.Id,
            StaffId = staffId,
            WitnessConfirmed = request.WitnessConfirmed,
            IdChecked = request.IdChecked,
            Notes = _encryption.Encrypt(notes),
            CompletedAt = now
        };

        await _handovers.CreateAsync(handover, cancellationToken);

        found.Status = ReportStatus.Returned;
        found.UpdatedAt = now;
        await _foundReports.UpdateAsync(found, cancellationToken);

        if (claim.LostReportId is { } lostId)
        {
            var lost = await _lostReports.GetByIdAsync(lostId, cancellationToken);
            if (lost is not null && lost.Status != ReportStatus.Returned)
            {
                lost.Status = ReportStatus.Returned;
                lost.UpdatedAt = now;
                await _lostReports.UpdateAsync(lost, cancellationToken);
            }
        }

        await _custody.RecordEventAsync(
            found.Id,
            CustodyEventType.HandedOver,
            staffId,
            UserRole.Staff,
            "Supervised handover completed with ID check and witness confirmation.",
            cancellationToken);

        _logger.LogInformation("Completed supervised handover {HandoverId} for claim {ClaimId}.", handover.Id, claim.Id);

        return ServiceResult<HandoverDto>.Success(
            new HandoverDto(
                handover.Id.ToString(),
                handover.ClaimId.ToString(),
                handover.FoundReportId.ToString(),
                handover.WitnessConfirmed,
                handover.IdChecked,
                handover.CompletedAt),
            StatusCodes.Status201Created);
    }

    private async Task<ServiceResult<StaffClaimReviewDto>> ReviewClaimAsync(
        string claimId,
        string staffUserId,
        bool approve,
        ReviewClaimRequest request,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(claimId, out var id) || !ObjectId.TryParse(staffUserId, out var staffId))
        {
            return ServiceResult<StaffClaimReviewDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        var claim = await _claims.GetByIdAsync(id, cancellationToken);
        if (claim is null)
        {
            return ServiceResult<StaffClaimReviewDto>.Failure(StatusCodes.Status404NotFound, "Claim not found.");
        }

        if (claim.Status != ClaimStatus.Pending)
        {
            return ServiceResult<StaffClaimReviewDto>.Failure(
                StatusCodes.Status409Conflict,
                "Only pending claims can be reviewed.");
        }

        var found = await _foundReports.GetByIdAsync(claim.FoundReportId, cancellationToken);
        if (found is null)
        {
            return ServiceResult<StaffClaimReviewDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        if (approve && found.Status is not (ReportStatus.Open or ReportStatus.Matched))
        {
            return ServiceResult<StaffClaimReviewDto>.Failure(
                StatusCodes.Status409Conflict,
                "Found item is already claimed or closed.");
        }

        var now = DateTime.UtcNow;
        var plainNotes = string.IsNullOrWhiteSpace(request.ReviewNotes) ? null : request.ReviewNotes.Trim();

        claim.Status = approve ? ClaimStatus.Approved : ClaimStatus.Rejected;
        claim.ReviewedBy = staffId;
        claim.ReviewNotes = plainNotes is null ? null : _encryption.Encrypt(plainNotes);
        claim.UpdatedAt = now;
        await _claims.UpdateAsync(claim, cancellationToken);

        if (approve)
        {
            found.Status = ReportStatus.Claimed;
            found.UpdatedAt = now;
            await _foundReports.UpdateAsync(found, cancellationToken);
        }

        await _custody.RecordEventAsync(
            found.Id,
            approve ? CustodyEventType.ClaimApproved : CustodyEventType.Rejected,
            staffId,
            UserRole.Staff,
            approve ? "Staff approved ownership claim." : "Staff rejected ownership claim.",
            cancellationToken);

        return ServiceResult<StaffClaimReviewDto>.Success(ToStaffReviewDto(claim, found));
    }

    private StaffClaimReviewDto ToStaffReviewDto(Claim claim, FoundReport? found)
    {
        return new StaffClaimReviewDto(
            Id: claim.Id.ToString(),
            FoundReportId: claim.FoundReportId.ToString(),
            LostReportId: claim.LostReportId?.ToString(),
            Status: claim.Status,
            VerificationAnswers: _encryption.Decrypt(claim.VerificationAnswers),
            FoundVerificationDetails: found is null ? string.Empty : _encryption.Decrypt(found.VerificationDetails),
            FoundStorageNotes: found is null ? string.Empty : _encryption.Decrypt(found.StorageNotes),
            ReviewNotes: claim.ReviewNotes is null ? null : _encryption.Decrypt(claim.ReviewNotes),
            CreatedAt: claim.CreatedAt,
            FoundItem: found is null ? null : ReportMappings.ToPublicDto(found));
    }

    private static ClaimDto ToClaimDto(
        Claim claim,
        string decryptedAnswers,
        string? decryptedReviewNotes,
        PublicReportDto? foundItem)
    {
        return new ClaimDto(
            Id: claim.Id.ToString(),
            FoundReportId: claim.FoundReportId.ToString(),
            LostReportId: claim.LostReportId?.ToString(),
            Status: claim.Status,
            VerificationAnswers: decryptedAnswers,
            ReviewNotes: decryptedReviewNotes,
            CreatedAt: claim.CreatedAt,
            FoundItem: foundItem);
    }
}
