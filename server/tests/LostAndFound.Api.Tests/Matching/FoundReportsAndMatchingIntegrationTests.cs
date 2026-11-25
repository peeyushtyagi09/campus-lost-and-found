using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Tests.Matching;

[Collection(ApiCollection.Name)]
public sealed class FoundReportsAndMatchingIntegrationTests
{
    private readonly ApiFactory _factory;

    public FoundReportsAndMatchingIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FoundReportAndMatching_NeverExposesPrivateFoundFieldsInBoardOrMatches()
    {
        var ownerClient = await CreateAuthenticatedClientAsync("Owner");
        var finderClient = await CreateAuthenticatedClientAsync("Finder");
        using var anonClient = _factory.CreateClient();
        using var _o = ownerClient;
        using var _f = finderClient;

        const string secretStorage = "LOCKER_B_BIN_14_SECRET";
        const string secretFinderContact = "finder.secret.phone.5559876";
        const string secretVerification = "ENGRAVED_INITIALS_KMP_992";

        var lostDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var createLost = await ownerClient.PostAsJsonAsync("/api/v1/lost-reports", new CreateLostReportRequest(
            "Titanium Water Bottle",
            ItemCategory.Other,
            "Insulated silver bottle left by reading tables.",
            "Library",
            lostDate,
            null,
            "Engraved initials KMP",
            "5551112222",
            "owner.bottle@campus.edu"));
        createLost.StatusCode.Should().Be(HttpStatusCode.Created);
        var lost = (await createLost.Content.ReadFromJsonAsync<LostReportOwnerDto>())!;

        var createFound = await finderClient.PostAsJsonAsync("/api/v1/found-reports", new CreateFoundReportRequest(
            "Silver Insulated Water Bottle",
            ItemCategory.Other,
            "Insulated silver bottle found near reading tables.",
            "Library",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Main Security Desk",
            null,
            secretStorage,
            secretFinderContact,
            secretVerification));
        createFound.StatusCode.Should().Be(HttpStatusCode.Created);
        var found = (await createFound.Content.ReadFromJsonAsync<FoundReportOwnerDto>())!;

        // Verify encrypted in Mongo
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
            var rawCol = context.FoundReports.Database.GetCollection<BsonDocument>(MongoContext.FoundReportsCollection);
            var rawDoc = await rawCol.Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(found.Id))).FirstOrDefaultAsync();
            rawDoc.Should().NotBeNull();
            rawDoc!["StorageNotes"].AsString.Should().NotBe(secretStorage);
            rawDoc["FinderContact"].AsString.Should().NotBe(secretFinderContact);
            rawDoc["VerificationDetails"].AsString.Should().NotBe(secretVerification);
        }

        // Public board with type=found shows HeldAt but zero private fields
        var foundBoardRes = await anonClient.GetAsync("/api/v1/public/board?type=found&q=Titanium%20Bottle");
        foundBoardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var foundBoardJson = await anonClient.GetStringAsync("/api/v1/public/board?type=found");
        foundBoardJson.Should().Contain("Main Security Desk");
        foundBoardJson.Should().NotContain(secretStorage);
        foundBoardJson.Should().NotContain(secretFinderContact);
        foundBoardJson.Should().NotContain(secretVerification);

        // Trigger on-demand matching and query /matches/mine as Owner
        var runRes = await ownerClient.PostAsync("/api/v1/matches/run", content: null);
        runRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var matchesRes = await ownerClient.GetAsync("/api/v1/matches/mine");
        matchesRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var matchesJson = await matchesRes.Content.ReadAsStringAsync();

        matchesJson.Should().NotContain(secretStorage);
        matchesJson.Should().NotContain(secretFinderContact);
        matchesJson.Should().NotContain(secretVerification);
        matchesJson.Should().NotContainEquivalentOf("storageNotes");
        matchesJson.Should().NotContainEquivalentOf("finderContact");
        matchesJson.Should().NotContainEquivalentOf("verificationDetails");

        var matches = (await matchesRes.Content.ReadFromJsonAsync<List<ItemMatchDto>>())!;
        var match = matches.Should().Contain(m => m.LostReportId == lost.Id && m.FoundReportId == found.Id).Subject;
        match.Score.Should().BeGreaterOrEqualTo(60);

        // Confirm match updates match and lost report status
        var confirmRes = await ownerClient.PostAsync($"/api/v1/matches/{match.Id}/confirm", content: null);
        confirmRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var confirmed = (await confirmRes.Content.ReadFromJsonAsync<ItemMatchDto>())!;
        confirmed.Status.Should().Be(MatchStatus.Confirmed);

        var updatedLost = await (await ownerClient.GetAsync($"/api/v1/lost-reports/{lost.Id}"))
            .Content.ReadFromJsonAsync<LostReportOwnerDto>();
        updatedLost!.Status.Should().Be(ReportStatus.Matched);

        // Finder cannot confirm/dismiss Owner's match (404)
        (await finderClient.PostAsync($"/api/v1/matches/{match.Id}/dismiss", content: null))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string name)
    {
        var client = _factory.CreateClient();
        var email = $"m2-{Guid.NewGuid():N}@campus.edu";
        const string password = "MatchPassword1";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(name, email, password));
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
