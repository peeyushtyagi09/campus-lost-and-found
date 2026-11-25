using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace LostAndFound.Api.Tests.EndToEnd;

[Collection(ApiCollection.Name)]
public sealed class EndToEndM1JourneyTests
{
    private readonly ApiFactory _factory;

    public EndToEndM1JourneyTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FullM1AcceptanceScenario_PreservesPrivacyAcrossBoardMineAndMedia()
    {
        using var clientA = _factory.CreateClient();
        using var clientB = _factory.CreateClient();
        using var anonClient = _factory.CreateClient();

        var emailA = $"usera-{Guid.NewGuid():N}@campus.edu";
        var emailB = $"userb-{Guid.NewGuid():N}@campus.edu";
        const string password = "AcceptancePass1";

        (await clientA.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("User A", emailA, password)))
            .StatusCode.Should().Be(HttpStatusCode.Created);
        (await clientB.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("User B", emailB, password)))
            .StatusCode.Should().Be(HttpStatusCode.Created);

        var loginA = await (await clientA.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(emailA, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        var loginB = await (await clientB.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(emailB, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();

        clientA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginA!.AccessToken);
        clientB.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginB!.AccessToken);

        // User A uploads photo with GPS EXIF
        using var image = new Image<Rgba32>(40, 40, Color.Navy);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(12, 1), new Rational(58, 1), new Rational(17, 1)]);
        image.Metadata.ExifProfile = exif;
        await using var ms = new MemoryStream();
        await image.SaveAsJpegAsync(ms);

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(ms.ToArray());
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        form.Add(fileContent, "file", "phone.jpg");

        var uploadRes = await clientA.PostAsync("/api/v1/uploads/photos", form);
        uploadRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploaded = (await uploadRes.Content.ReadFromJsonAsync<UploadPhotoResponse>())!;

        const string secretMarks = "E2E_CRACKED_CORNER_SN_8811";
        const string secretPhone = "555-246-8135";
        const string secretEmail = "e2e.private@campus.edu";

        var createRes = await clientA.PostAsJsonAsync("/api/v1/lost-reports", new CreateLostReportRequest(
            "E2E Lost Phone",
            ItemCategory.Phone,
            "Dark blue phone left near library desk.",
            "Library",
            DateOnly.FromDateTime(DateTime.UtcNow),
            uploaded.FileName,
            secretMarks,
            secretPhone,
            secretEmail));
        createRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createRes.Content.ReadFromJsonAsync<LostReportOwnerDto>())!;

        // Anonymous public board shows item without private details, contact info, or reporterId
        var boardRes = await anonClient.GetAsync("/api/v1/public/board?q=E2E%20Lost%20Phone");
        boardRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var boardJson = await boardRes.Content.ReadAsStringAsync();
        boardJson.Should().Contain("E2E Lost Phone");
        boardJson.Should().NotContain(secretMarks);
        boardJson.Should().NotContain(secretPhone);
        boardJson.Should().NotContain(secretEmail);
        boardJson.Should().NotContain(loginA.User.Id);

        var board = (await boardRes.Content.ReadFromJsonAsync<PagedResponse<PublicReportDto>>())!;
        var item = board.Items.Should().ContainSingle(r => r.Id == created.Id).Subject;
        item.PhotoUrl.Should().Be(uploaded.Url);

        // Downloaded photo has no EXIF
        var downloadedPhoto = await anonClient.GetByteArrayAsync(item.PhotoUrl);
        using var loadedPhoto = Image.Load(downloadedPhoto);
        loadedPhoto.Metadata.ExifProfile.Should().BeNull();

        // User A sees private details in /mine
        var mine = await (await clientA.GetAsync("/api/v1/lost-reports/mine"))
            .Content.ReadFromJsonAsync<PagedResponse<LostReportOwnerDto>>();
        mine!.Items.Should().ContainSingle(r => r.Id == created.Id && r.PrivateDetails == secretMarks);

        // User B cannot access User A's report (404)
        (await clientB.GetAsync($"/api/v1/lost-reports/{created.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AuthEndpoint_ExceedingPermitLimit_Returns429ProblemDetails()
    {
        using var limitedFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["RateLimiting:AuthPermitLimit"] = "2",
                    ["RateLimiting:GlobalPermitLimit"] = "100",
                    ["RateLimiting:WindowSeconds"] = "60"
                });
            });
        });

        using var client = limitedFactory.CreateClient();
        var req = new LoginRequest("nobody@campus.edu", "WrongPassword1");

        (await client.PostAsJsonAsync("/api/v1/auth/login", req)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await client.PostAsJsonAsync("/api/v1/auth/login", req)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var throttled = await client.PostAsJsonAsync("/api/v1/auth/login", req);
        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        throttled.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }
}
