using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Tests.Reports;

[Collection(ApiCollection.Name)]
public sealed class LostReportsIntegrationTests
{
    private readonly ApiFactory _factory;

    public LostReportsIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateReport_IgnoresBodyReporterId_AndEncryptsPrivateFieldsInMongo()
    {
        var (clientA, userAId) = await CreateAuthenticatedUserAsync("Owner A");
        using var _ = clientA;
        var spoofedReporterId = ObjectId.GenerateNewId().ToString();

        const string secretDetails = "IMEI-998877665544-UNIQUE-STICKER";
        const string secretPhone = "+15551234567";
        const string secretEmail = "ownera.private@campus.edu";

        var createBody = new
        {
            reporterId = spoofedReporterId,
            title = "Black Pixel Phone",
            category = "Phone",
            publicDescription = "Lost near main library entrance with dark case.",
            campusArea = "Library",
            dateLost = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"),
            photoFileName = (string?)null,
            privateDetails = secretDetails,
            contactPhone = secretPhone,
            contactEmail = secretEmail
        };

        var createResponse = await clientA.PostAsJsonAsync("/api/v1/lost-reports", createBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<LostReportOwnerDto>();
        created.Should().NotBeNull();
        created!.PrivateDetails.Should().Be(secretDetails);
        created.ContactPhone.Should().Be(secretPhone);
        created.ContactEmail.Should().Be(secretEmail);

        // Read raw BSON document directly from Mongo to verify encryption at rest and ReporterId enforcement
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MongoContext>();
        var rawCollection = context.LostReports.Database.GetCollection<BsonDocument>(MongoContext.LostReportsCollection);
        var rawDoc = await rawCollection
            .Find(Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Parse(created.Id)))
            .FirstOrDefaultAsync();

        rawDoc.Should().NotBeNull();
        rawDoc!["ReporterId"].AsObjectId.ToString().Should().Be(userAId);
        rawDoc["ReporterId"].AsObjectId.ToString().Should().NotBe(spoofedReporterId);
        rawDoc["PrivateDetails"].AsString.Should().NotBe(secretDetails);
        rawDoc["ContactPhone"].AsString.Should().NotBe(secretPhone);
        rawDoc["ContactEmail"].AsString.Should().NotBe(secretEmail);
    }

    [Fact]
    public async Task OwnerIsolation_OtherUserReceives404OnReadUpdateDelete_StaffCanReadAndClose()
    {
        var (clientA, _) = await CreateAuthenticatedUserAsync("Owner A");
        var (clientB, _) = await CreateAuthenticatedUserAsync("Owner B");
        using var _a = clientA;
        using var _b = clientB;

        var createResponse = await clientA.PostAsJsonAsync(
            "/api/v1/lost-reports",
            new CreateLostReportRequest(
                "Silver Laptop",
                ItemCategory.Laptop,
                "Left in lecture hall row 3",
                "Engineering Block",
                DateOnly.FromDateTime(DateTime.UtcNow),
                null,
                "Asset tag 441122",
                "9876543210",
                "laptop.owner@campus.edu"));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<LostReportOwnerDto>())!;

        // Owner A sees it in /mine and /{id}
        var mine = await (await clientA.GetAsync("/api/v1/lost-reports/mine")).Content
            .ReadFromJsonAsync<PagedResponse<LostReportOwnerDto>>();
        mine!.Items.Should().Contain(item => item.Id == created.Id);

        (await clientA.GetAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);

        // Owner B gets 404 (not 403) on GET, PUT, DELETE
        (await clientB.GetAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.PutAsJsonAsync($"/api/v1/lost-reports/{created.Id}", new UpdateLostReportRequest(
            "Hijacked",
            ItemCategory.Laptop,
            "Trying to modify",
            "Engineering Block",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            "x",
            "9876543210",
            "b@campus.edu"))).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await clientB.DeleteAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Owner A updates successfully
        var updateResponse = await clientA.PutAsJsonAsync($"/api/v1/lost-reports/{created.Id}", new UpdateLostReportRequest(
            "Silver 14-inch Laptop",
            ItemCategory.Laptop,
            "Updated description without contact info",
            "Engineering Block",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            "Asset tag 441122 updated",
            "9876543210",
            "laptop.owner@campus.edu"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Staff can read and soft-close
        using var staffClient = await CreateStaffClientAsync();
        (await staffClient.GetAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await staffClient.DeleteAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterClose = await (await clientA.GetAsync($"/api/v1/lost-reports/{created.Id}"))
            .Content.ReadFromJsonAsync<LostReportOwnerDto>();
        afterClose!.Status.Should().Be(ReportStatus.Closed);
    }

    [Theory]
    [InlineData("Call me at 9876543210 if found")]
    [InlineData("Reach me via test.user@campus.edu")]
    [InlineData("Phone (555) 123-4567")]
    public async Task CreateReport_ContactInfoInPublicDescription_ReturnsBadRequest(string publicDescriptionWithContact)
    {
        var (client, _) = await CreateAuthenticatedUserAsync("Validator Test");
        using var _ = client;

        var response = await client.PostAsJsonAsync(
            "/api/v1/lost-reports",
            new CreateLostReportRequest(
                "Blue Wallet",
                ItemCategory.Wallet,
                publicDescriptionWithContact,
                "Cafeteria",
                DateOnly.FromDateTime(DateTime.UtcNow),
                null,
                "Contains student ID",
                "5551234567",
                "wallet@campus.edu"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<(HttpClient Client, string UserId)> CreateAuthenticatedUserAsync(string name)
    {
        var client = _factory.CreateClient();
        var email = $"reporter-{Guid.NewGuid():N}@campus.edu";
        const string password = "ReportPassword1";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest(name, email, password));
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return (client, login.User.Id);
    }

    private async Task<HttpClient> CreateStaffClientAsync()
    {
        var email = $"staff-rep-{Guid.NewGuid():N}@campus.edu";
        const string password = "StaffPassword1";

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var emailHasher = scope.ServiceProvider.GetRequiredService<IEmailHasher>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

            var normalized = emailHasher.Normalize(email);
            var staff = new User
            {
                Id = ObjectId.GenerateNewId(),
                DisplayName = "Campus Staff",
                Email = encryption.Encrypt(normalized),
                EmailHash = emailHasher.Hash(normalized),
                Role = UserRole.Staff,
                CreatedAt = DateTime.UtcNow
            };
            staff.PasswordHash = passwordHasher.HashPassword(staff, password);
            await users.CreateAsync(staff, CancellationToken.None);
        }

        var client = _factory.CreateClient();
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
