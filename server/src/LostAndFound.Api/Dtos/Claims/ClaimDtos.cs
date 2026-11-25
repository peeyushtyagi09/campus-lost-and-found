using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Dtos.Claims;

public sealed record CreateClaimRequest(
    string FoundReportId,
    string? LostReportId,
    string VerificationAnswers);

public sealed record ReviewClaimRequest(string? ReviewNotes);

public sealed record CompleteHandoverRequest(
    bool WitnessConfirmed,
    bool IdChecked,
    string? Notes);

public sealed record ClaimDto(
    string Id,
    string FoundReportId,
    string? LostReportId,
    ClaimStatus Status,
    string VerificationAnswers,
    string? ReviewNotes,
    DateTime CreatedAt,
    PublicReportDto? FoundItem);

public sealed record StaffClaimReviewDto(
    string Id,
    string FoundReportId,
    string? LostReportId,
    ClaimStatus Status,
    string VerificationAnswers,
    string FoundVerificationDetails,
    string FoundStorageNotes,
    string? ReviewNotes,
    DateTime CreatedAt,
    PublicReportDto? FoundItem);

public sealed record HandoverDto(
    string Id,
    string ClaimId,
    string FoundReportId,
    bool WitnessConfirmed,
    bool IdChecked,
    DateTime CompletedAt);

public sealed record CustodyEventDto(
    string Id,
    string ReportId,
    long SequenceNumber,
    CustodyEventType EventType,
    UserRole ActorRole,
    DateTime Timestamp,
    string Notes,
    string PreviousEventHash,
    string EventHash);

public sealed record CustodyTimelineDto(
    string ReportId,
    bool IsChainValid,
    IReadOnlyList<CustodyEventDto> Events);
