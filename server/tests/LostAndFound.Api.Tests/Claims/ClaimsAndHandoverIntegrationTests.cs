using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace LostAndFound.Api.Tests.Claims;

[Collection(ApiCollection.Name)]
public sealed class ClaimsAndHandoverIntegrationTests
{
    private readonly ApiFactory _factory;

    public ClaimsAndHandoverIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ClaimApprovalAndHandover_EnforcesRolesStateMachineAndHashChain()
    {
        var claimantClient = await CreateUserClientAsync("Claimant", UserRole.User);
        var staffClient = await CreateUserClientAsync("Desk Staff", UserRole.Staff);
        var outsiderClient = await CreateUserClientAsync("Outsider", UserRole.User);
        using var _c = claimantClient;
        using var _s = staffClient;
        using var _o = outsiderClient;

        // Staff logs found item
        var createFound = await staffClient.PostAsJsonAsync("/api/v1/found-reports", new CreateFoundReportRequest(
            "Found Graphing Calculator",
            ItemCategory.Other,
            "Black calculator left in Math Hall",
            "Science Building",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Main Security Desk",
            null,
            "Cabinet 2 Drawer B",
            "staff@campus.edu",
            "Sticker on battery cover says PHY-101"));
        createFound.StatusCode.Should().Be(HttpStatusCode.Created);
        var found = (await createFound.Content.ReadFromJsonAsync<FoundReportOwnerDto>())!;

        // Claimant submits claim
        var submitClaim = await claimantClient.PostAsJsonAsync("/api/v1/claims", new CreateClaimRequest(
            found.Id,
            null,
            "Battery cover has a PHY-101 sticker."));
        submitClaim.StatusCode.Should().Be(HttpStatusCode.Created);
        var claim = (await submitClaim.Content.ReadFromJsonAsync<ClaimDto>())!;

        // Illegal state transition: cannot complete handover before approval (409)
        (await staffClient.PostAsJsonAsync($"/api/v1/handovers/{claim.Id}/complete",
            new CompleteHandoverRequest(true, true, "early"))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Role enforcement: regular user cannot access staff review or approve (403)
        (await claimantClient.GetAsync("/api/v1/claims/review")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await claimantClient.PostAsJsonAsync($"/api/v1/claims/{claim.Id}/approve",
            new ReviewClaimRequest("self approve"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await claimantClient.PostAsJsonAsync($"/api/v1/handovers/{claim.Id}/complete",
            new CompleteHandoverRequest(true, true, "self handover"))).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // Staff views review queue (sees private verification details) and approves
        var queue = await (await staffClient.GetAsync("/api/v1/claims/review?status=Pending"))
            .Content.ReadFromJsonAsync<List<StaffClaimReviewDto>>();
        var reviewItem = queue!.Should().ContainSingle(x => x.Id == claim.Id).Subject;
        reviewItem.FoundVerificationDetails.Should().Contain("PHY-101");
        reviewItem.FoundStorageNotes.Should().Contain("Cabinet 2 Drawer B");

        var approveRes = await staffClient.PostAsJsonAsync($"/api/v1/claims/{claim.Id}/approve",
            new ReviewClaimRequest("Verified PHY-101 sticker."));
        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Illegal transition: cannot withdraw or reject after approval (409)
        (await claimantClient.PostAsync($"/api/v1/claims/{claim.Id}/withdraw", null))
            .StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await staffClient.PostAsJsonAsync($"/api/v1/claims/{claim.Id}/reject",
            new ReviewClaimRequest("too late"))).StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Handover fails validation if WitnessConfirmed or IdChecked is false (400)
        (await staffClient.PostAsJsonAsync($"/api/v1/handovers/{claim.Id}/complete",
            new CompleteHandoverRequest(false, true, "no witness"))).StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Staff completes handover
        var handoverRes = await staffClient.PostAsJsonAsync($"/api/v1/handovers/{claim.Id}/complete",
            new CompleteHandoverRequest(true, true, "Handed to claimant after ID check."));
        handoverRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Found report is now Returned
        var updatedFound = await (await staffClient.GetAsync($"/api/v1/found-reports/{found.Id}"))
            .Content.ReadFromJsonAsync<FoundReportOwnerDto>();
        updatedFound!.Status.Should().Be(ReportStatus.Returned);

        // Custody timeline is visible to claimant & staff (valid hash chain, no actorId), 404 for outsider
        (await outsiderClient.GetAsync($"/api/v1/custody/{found.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var timelineRes = await claimantClient.GetAsync($"/api/v1/custody/{found.Id}");
        timelineRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var timelineJson = await timelineRes.Content.ReadAsStringAsync();
        timelineJson.Should().NotContainEquivalentOf("actorId");

        var timeline = (await timelineRes.Content.ReadFromJsonAsync<CustodyTimelineDto>())!;
        timeline.IsChainValid.Should().BeTrue();
        timeline.Events.Select(e => e.EventType).Should().ContainInOrder(
            CustodyEventType.Received,
            CustodyEventType.Stored,
            CustodyEventType.ClaimSubmitted,
            CustodyEventType.ClaimApproved,
            CustodyEventType.HandedOver);
    }

    private async Task<HttpClient> CreateUserClientAsync(string name, UserRole role)
    {
        var email = $"m3-{Guid.NewGuid():N}@campus.edu";
        const string password = "CustodyPassword1";

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var emailHasher = scope.ServiceProvider.GetRequiredService<IEmailHasher>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var normalized = emailHasher.Normalize(email);
            var user = new User
            {
                Id = ObjectId.GenerateNewId(),
                DisplayName = name,
                Email = encryption.Encrypt(normalized),
                EmailHash = emailHasher.Hash(normalized),
                Role = role,
                CreatedAt = DateTime.UtcNow
            };
            user.PasswordHash = passwordHasher.HashPassword(user, password);
            await users.CreateAsync(user, CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
