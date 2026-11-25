using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using MongoDB.Bson;
using NSubstitute;

namespace LostAndFound.Api.Tests.Claims;

public sealed class CustodyChainTests
{
    [Fact]
    public void VerifyChain_ValidChainPasses_TamperingBreaksVerification()
    {
        var service = new CustodyService(
            Substitute.For<ICustodyEventRepository>(),
            Substitute.For<IFoundReportRepository>(),
            Substitute.For<IClaimRepository>());

        var reportId = ObjectId.GenerateNewId();
        var actorId = ObjectId.GenerateNewId();
        var t0 = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

        var e1 = new CustodyEvent
        {
            Id = ObjectId.GenerateNewId(),
            ReportId = reportId,
            SequenceNumber = 1,
            EventType = CustodyEventType.Received,
            ActorId = actorId,
            ActorRole = UserRole.User,
            Timestamp = t0,
            Notes = "Received at desk",
            PreviousEventHash = CustodyService.GenesisHash
        };
        e1.EventHash = CustodyService.ComputeHash(e1);

        var e2 = new CustodyEvent
        {
            Id = ObjectId.GenerateNewId(),
            ReportId = reportId,
            SequenceNumber = 2,
            EventType = CustodyEventType.ClaimApproved,
            ActorId = actorId,
            ActorRole = UserRole.Staff,
            Timestamp = t0.AddMinutes(5),
            Notes = "Claim approved",
            PreviousEventHash = e1.EventHash
        };
        e2.EventHash = CustodyService.ComputeHash(e2);

        service.VerifyChain([e1, e2]).Should().BeTrue();

        // Tamper with one event's Notes
        e1.Notes = "Tampered notes";
        service.VerifyChain([e1, e2]).Should().BeFalse();
    }
}
