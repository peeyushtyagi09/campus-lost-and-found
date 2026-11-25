using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Claims;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace LostAndFound.Api.Tests.Admin;

[Collection(ApiCollection.Name)]
public sealed class DisposalNotificationsAdminTests
{
    private readonly ApiFactory _factory;

    public DisposalNotificationsAdminTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DisposalSweepAndApproval_DeletesPhotoWritesCustodyEventAndAuditLog()
    {
        var staffClient = await CreateRoleClientAsync("Disposal Staff", UserRole.Staff);
        using var _s = staffClient;

        // Upload photo first
        using var img = new Image<Rgba32>(20, 20, Color.Coral);
        await using var ms = new MemoryStream();
        await img.SaveAsJpegAsync(ms);
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(fileContent, "file", "old-umbrella.jpg");
        var uploaded = (await (await staffClient.PostAsync("/api/v1/uploads/photos", form))
            .Content.ReadFromJsonAsync<UploadPhotoResponse>())!;

        // Insert a 100-day-old FoundReport directly via repository
        var oldFoundId = ObjectId.GenerateNewId();
        using (var scope = _factory.Services.CreateScope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IFoundReportRepository>();
            var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var custody = scope.ServiceProvider.GetRequiredService<ICustodyService>();
            var createdAt = DateTime.UtcNow.AddDays(-100);
            var report = new FoundReport
            {
                Id = oldFoundId,
                FinderId = ObjectId.GenerateNewId(),
                Title = "Aged Unclaimed Scarf",
                Category = ItemCategory.Clothing,
                PublicDescription = "Wool scarf unclaimed after retention window",
                CampusArea = "Library",
                DateFound = createdAt.Date,
                HeldAt = "Main Security Desk",
                PhotoFileName = uploaded.FileName,
                Status = ReportStatus.Open,
                StorageNotes = encryption.Encrypt("Shelf 9"),
                FinderContact = encryption.Encrypt("finder@campus.edu"),
                VerificationDetails = encryption.Encrypt("Tartan pattern"),
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };
            await repo.CreateAsync(report, CancellationToken.None);
            await custody.RecordEventAsync(oldFoundId, CustodyEventType.Received, report.FinderId, UserRole.User, "Received", CancellationToken.None);

            var disposalService = scope.ServiceProvider.GetRequiredService<IDisposalService>();
            var flagged = await disposalService.FlagExpiredFoundItemsAsync(CancellationToken.None);
            flagged.Should().BeGreaterOrEqualTo(1);
        }

        // Staff sees eligible list and approves disposal
        var eligible = await (await staffClient.GetAsync("/api/v1/disposals/eligible"))
            .Content.ReadFromJsonAsync<List<PublicReportDto>>();
        eligible!.Should().Contain(item => item.Id == oldFoundId.ToString());

        var approveRes = await staffClient.PostAsJsonAsync(
            $"/api/v1/disposals/{oldFoundId}/approve",
            new ApproveDisposalRequest(DisposalMethod.Donate, "Donated to campus charity drive."));
        approveRes.StatusCode.Should().Be(HttpStatusCode.Created);

        // Stored photo deleted from disk and media endpoint returns 404
        using (var scope = _factory.Services.CreateScope())
        {
            var storageOpts = scope.ServiceProvider.GetRequiredService<IOptions<StorageOptions>>().Value;
            File.Exists(Path.Combine(storageOpts.UploadPath, uploaded.FileName)).Should().BeFalse();
        }
        (await staffClient.GetAsync(uploaded.Url)).StatusCode.Should().Be(HttpStatusCode.NotFound);

        // Custody timeline includes Disposed and valid hash chain
        var timeline = await (await staffClient.GetAsync($"/api/v1/custody/{oldFoundId}"))
            .Content.ReadFromJsonAsync<CustodyTimelineDto>();
        timeline!.IsChainValid.Should().BeTrue();
        timeline.Events.Should().Contain(e => e.EventType == CustodyEventType.Disposed);
    }

    [Fact]
    public async Task Notifications_MatchCreatesNotification_AndMarkReadWorks()
    {
        var userClient = await CreateRoleClientAsync("Notif User", UserRole.User);
        var staffClient = await CreateRoleClientAsync("Notif Staff", UserRole.Staff);
        using var _u = userClient;
        using var _s = staffClient;

        await userClient.PostAsJsonAsync("/api/v1/lost-reports", new CreateLostReportRequest(
            "Unique Titanium Pen",
            ItemCategory.Other,
            "Silver titanium pen lost near library.",
            "Library",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            null,
            "Engraved NTP",
            "5559998888",
            "notif@campus.edu"));

        await staffClient.PostAsJsonAsync("/api/v1/found-reports", new CreateFoundReportRequest(
            "Found Titanium Pen",
            ItemCategory.Other,
            "Silver titanium pen near library.",
            "Library",
            DateOnly.FromDateTime(DateTime.UtcNow),
            "Main Security Desk",
            null,
            "Drawer 1",
            "staff@campus.edu",
            "Engraved NTP"));

        (await userClient.PostAsync("/api/v1/matches/run", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var notifications = await (await userClient.GetAsync("/api/v1/notifications"))
            .Content.ReadFromJsonAsync<List<NotificationDto>>();
        notifications.Should().NotBeEmpty();
        var notif = notifications![0];
        notif.IsRead.Should().BeFalse();
        notif.Message.Should().NotContain("5559998888");
        notif.Message.Should().NotContain("Engraved NTP");

        var readRes = await userClient.PostAsync($"/api/v1/notifications/{notif.Id}/read", null);
        readRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await readRes.Content.ReadFromJsonAsync<NotificationDto>())!;
        updated.IsRead.Should().BeTrue();
    }

    [Fact]
    public async Task AdminEndpoints_CreateStaffAndReadAuditLogs_RejectNonAdmins()
    {
        var adminClient = await CreateRoleClientAsync("Root Admin", UserRole.Admin);
        var staffClient = await CreateRoleClientAsync("Regular Staff", UserRole.Staff);
        using var _a = adminClient;
        using var _s = staffClient;

        (await staffClient.GetAsync("/api/v1/admin/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var staffEmail = $"newstaff-{Guid.NewGuid():N}@campus.edu";
        var createRes = await adminClient.PostAsJsonAsync(
            "/api/v1/admin/staff",
            new CreateStaffRequest("New Desk Staff", staffEmail, "StaffSecret123"));
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createRes.Content.ReadFromJsonAsync<AdminUserDto>())!;
        created.Role.Should().Be(UserRole.Staff);

        var logs = await (await adminClient.GetAsync("/api/v1/admin/audit-logs"))
            .Content.ReadFromJsonAsync<PagedResponse<AuditEventDto>>();
        logs!.Items.Should().Contain(e => e.Action == "StaffAccountCreated" && e.TargetId == created.Id);

        // Metrics endpoint responds with Prometheus counters
        using var anon = _factory.CreateClient();
        var metricsRes = await anon.GetAsync("/metrics");
        metricsRes.StatusCode.Should().Be(HttpStatusCode.OK);
        (await metricsRes.Content.ReadAsStringAsync()).Should().Contain("lost_and_found_http_requests_total");
    }

    private async Task<HttpClient> CreateRoleClientAsync(string name, UserRole role)
    {
        var email = $"m4-{Guid.NewGuid():N}@campus.edu";
        const string password = "HardeningPassword1";

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
