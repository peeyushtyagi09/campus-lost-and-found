using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface ICustodyService
{
    Task<CustodyEvent> RecordEventAsync(
        ObjectId reportId,
        CustodyEventType eventType,
        ObjectId actorId,
        UserRole actorRole,
        string notes,
        CancellationToken cancellationToken);

    bool VerifyChain(IReadOnlyList<CustodyEvent> events);

    Task<ServiceResult<CustodyTimelineDto>> GetTimelineAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken);
}

public sealed class CustodyService : ICustodyService
{
    public const string GenesisHash = "GENESIS";
    private readonly ICustodyEventRepository _events;
    private readonly IFoundReportRepository _foundReports;
    private readonly IClaimRepository _claims;

    public CustodyService(
        ICustodyEventRepository events,
        IFoundReportRepository foundReports,
        IClaimRepository claims)
    {
        _events = events;
        _foundReports = foundReports;
        _claims = claims;
    }

    public async Task<CustodyEvent> RecordEventAsync(
        ObjectId reportId,
        CustodyEventType eventType,
        ObjectId actorId,
        UserRole actorRole,
        string notes,
        CancellationToken cancellationToken)
    {
        var existing = await _events.GetByReportIdAsync(reportId, cancellationToken);
        var last = existing.Count > 0 ? existing[^1] : null;
        var seq = last is null ? 1 : last.SequenceNumber + 1;
        var previousHash = last is null ? GenesisHash : last.EventHash;

        // Normalize timestamp to millisecond precision so MongoDB BSON round-trip preserves exact hash inputs
        var now = DateTime.UtcNow;
        var truncatedTimestamp = new DateTime(now.Ticks - (now.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
        var sanitizedNotes = string.IsNullOrWhiteSpace(notes) ? eventType.ToString() : notes.Trim();

        var custodyEvent = new CustodyEvent
        {
            Id = ObjectId.GenerateNewId(),
            ReportId = reportId,
            SequenceNumber = seq,
            EventType = eventType,
            ActorId = actorId,
            ActorRole = actorRole,
            Timestamp = truncatedTimestamp,
            Notes = sanitizedNotes,
            PreviousEventHash = previousHash
        };
        custodyEvent.EventHash = ComputeHash(custodyEvent);

        await _events.AppendAsync(custodyEvent, cancellationToken);
        return custodyEvent;
    }

    public bool VerifyChain(IReadOnlyList<CustodyEvent> events)
    {
        if (events.Count == 0)
        {
            return true;
        }

        var expectedPrevious = GenesisHash;
        long expectedSequence = 1;

        foreach (var item in events)
        {
            if (item.SequenceNumber != expectedSequence)
            {
                return false;
            }

            if (!string.Equals(item.PreviousEventHash, expectedPrevious, StringComparison.Ordinal))
            {
                return false;
            }

            var recomputed = ComputeHash(item);
            if (!string.Equals(item.EventHash, recomputed, StringComparison.Ordinal))
            {
                return false;
            }

            expectedPrevious = item.EventHash;
            expectedSequence++;
        }

        return true;
    }

    public async Task<ServiceResult<CustodyTimelineDto>> GetTimelineAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(reportId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<CustodyTimelineDto>.Failure(StatusCodes.Status404NotFound, "Custody record not found.");
        }

        var found = await _foundReports.GetByIdAsync(id, cancellationToken);
        if (found is null)
        {
            return ServiceResult<CustodyTimelineDto>.Failure(StatusCodes.Status404NotFound, "Custody record not found.");
        }

        var isAuthorized = isStaff || found.FinderId == callerId;
        if (!isAuthorized)
        {
            var claims = await _claims.GetByFoundReportAsync(id, cancellationToken);
            isAuthorized = claims.Any(c => c.ClaimantId == callerId);
        }

        if (!isAuthorized)
        {
            return ServiceResult<CustodyTimelineDto>.Failure(StatusCodes.Status404NotFound, "Custody record not found.");
        }

        var events = await _events.GetByReportIdAsync(id, cancellationToken);
        var isValid = VerifyChain(events);
        var dtos = events.Select(e => new CustodyEventDto(
            e.Id.ToString(),
            e.ReportId.ToString(),
            e.SequenceNumber,
            e.EventType,
            e.ActorRole,
            e.Timestamp,
            e.Notes,
            e.PreviousEventHash,
            e.EventHash)).ToList();

        return ServiceResult<CustodyTimelineDto>.Success(new CustodyTimelineDto(id.ToString(), isValid, dtos));
    }

    public static string ComputeHash(CustodyEvent custodyEvent)
    {
        var payload = string.Join(
            "|",
            custodyEvent.PreviousEventHash,
            custodyEvent.ReportId.ToString(),
            custodyEvent.SequenceNumber.ToString(CultureInfo.InvariantCulture),
            custodyEvent.EventType.ToString(),
            custodyEvent.ActorId.ToString(),
            custodyEvent.ActorRole.ToString(),
            custodyEvent.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
            custodyEvent.Notes);

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
