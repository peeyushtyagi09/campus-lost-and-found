using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Tests.Infrastructure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;

namespace LostAndFound.Api.Tests.Privacy;

[Collection(ApiCollection.Name)]
public sealed class PrivacyTests
{
    private readonly ApiFactory _factory;

    public PrivacyTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void PublicReportDto_ContainsOnlyPropertiesMappedFromPublicEntityFields()
    {
        var dtoToEntityMap = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(PublicReportDto.Id)] = nameof(LostReport.Id),
            [nameof(PublicReportDto.Title)] = nameof(LostReport.Title),
            [nameof(PublicReportDto.Category)] = nameof(LostReport.Category),
            [nameof(PublicReportDto.PublicDescription)] = nameof(LostReport.PublicDescription),
            [nameof(PublicReportDto.CampusArea)] = nameof(LostReport.CampusArea),
            [nameof(PublicReportDto.DateLost)] = nameof(LostReport.DateLost),
            [nameof(PublicReportDto.PhotoUrl)] = nameof(LostReport.PhotoFileName),
            [nameof(PublicReportDto.Status)] = nameof(LostReport.Status),
            [nameof(PublicReportDto.PostedAt)] = nameof(LostReport.CreatedAt)
        };

        var dtoProperties = typeof(PublicReportDto).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var dtoProperty in dtoProperties)
        {
            dtoToEntityMap.Should().ContainKey(
                dtoProperty.Name,
                $"{nameof(PublicReportDto)}.{dtoProperty.Name} must explicitly map from a Public LostReport property.");

            var sourceName = dtoToEntityMap[dtoProperty.Name];
            var entityProperty = typeof(LostReport).GetProperty(sourceName);
            entityProperty.Should().NotBeNull();

            var visibility = entityProperty!.GetCustomAttribute<FieldVisibilityAttribute>();
            visibility.Should().NotBeNull();
            visibility!.Visibility.Should().Be(
                Visibility.Public,
                $"{nameof(PublicReportDto)}.{dtoProperty.Name} cannot expose Private entity field {sourceName}.");
        }
    }

    [Fact]
    public async Task PublicBoard_ExcludesAllPrivateFieldsAndStrings_AndServedPhotoHasNoExif()
    {
        const string secretDetails = "PRIVATE_SERIAL_NUMBER_XYZ987";
        const string secretPhone = "555-999-0142";
        const string secretEmail = "hidden.owner@campus.edu";
        const string uniqueTitle = "Privacy Board Test Backpack";

        using var authClient = await CreateAuthenticatedClientAsync();

        // Upload GPS-tagged photo
        using var image = new Image<Rgba32>(48, 48, Color.DarkCyan);
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.GPSLatitudeRef, "N");
        exif.SetValue(ExifTag.GPSLatitude, [new Rational(37, 1), new Rational(25, 1), new Rational(19, 1)]);
        exif.SetValue(ExifTag.GPSLongitudeRef, "W");
        exif.SetValue(ExifTag.GPSLongitude, [new Rational(122, 1), new Rational(5, 1), new Rational(6, 1)]);
        image.Metadata.ExifProfile = exif;

        await using var photoStream = new MemoryStream();
        await image.SaveAsJpegAsync(photoStream);

        using var uploadForm = new MultipartFormDataContent();
        using var photoContent = new ByteArrayContent(photoStream.ToArray());
        photoContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        uploadForm.Add(photoContent, "file", "gps-backpack.jpg");

        var uploadRes = await authClient.PostAsync("/api/v1/uploads/photos", uploadForm);
        uploadRes.StatusCode.Should().Be(HttpStatusCode.Created);
        var uploaded = (await uploadRes.Content.ReadFromJsonAsync<UploadPhotoResponse>())!;

        // Create Open lost report + a Closed lost report (Closed must not appear on board)
        var createOpen = await authClient.PostAsJsonAsync("/api/v1/lost-reports", new CreateLostReportRequest(
            uniqueTitle,
            ItemCategory.Bag,
            "Navy canvas backpack left near student union.",
            "Student Union",
            DateOnly.FromDateTime(DateTime.UtcNow),
            uploaded.FileName,
            secretDetails,
            secretPhone,
            secretEmail));
        createOpen.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdOpen = (await createOpen.Content.ReadFromJsonAsync<LostReportOwnerDto>())!;

        var createClosed = await authClient.PostAsJsonAsync("/api/v1/lost-reports", new CreateLostReportRequest(
            "Closed Hidden Backpack",
            ItemCategory.Bag,
            "Should not be shown after closing.",
            "Student Union",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            "CLOSED_SECRET",
            "5550001111",
            "closed@campus.edu"));
        var createdClosed = (await createClosed.Content.ReadFromJsonAsync<LostReportOwnerDto>())!;
        (await authClient.DeleteAsync($"/api/v1/lost-reports/{createdClosed.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Anonymous access to board succeeds and leaks nothing
        using var anonClient = _factory.CreateClient();
        var boardRes = await anonClient.GetAsync("/api/v1/public/board?category=Bag&area=student%20union&q=Privacy%20Board&page=1&pageSize=20");
        boardRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var rawJson = await boardRes.Content.ReadAsStringAsync();
        rawJson.Should().NotContainEquivalentOf("contactPhone");
        rawJson.Should().NotContainEquivalentOf("contactEmail");
        rawJson.Should().NotContainEquivalentOf("privateDetails");
        rawJson.Should().NotContainEquivalentOf("reporterId");
        rawJson.Should().NotContainEquivalentOf("updatedAt");
        rawJson.Should().NotContainEquivalentOf("passwordHash");
        rawJson.Should().NotContainEquivalentOf("emailHash");
        rawJson.Should().NotContain(secretDetails);
        rawJson.Should().NotContain(secretPhone);
        rawJson.Should().NotContain(secretEmail);
        rawJson.Should().NotContainEquivalentOf("exif");
        rawJson.Should().NotContainEquivalentOf("gps");
        rawJson.Should().NotContain(createdClosed.Id);

        var board = await boardRes.Content.ReadFromJsonAsync<PagedResponse<PublicReportDto>>();
        board.Should().NotBeNull();
        board!.Items.Should().Contain(item => item.Id == createdOpen.Id && item.PhotoUrl == uploaded.Url);
        board.Items.Should().NotContain(item => item.Id == createdClosed.Id);

        // Served photo from /media has no EXIF/GPS
        var mediaRes = await anonClient.GetAsync(uploaded.Url);
        mediaRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var servedBytes = await mediaRes.Content.ReadAsByteArrayAsync();
        using var servedImage = Image.Load(servedBytes);
        servedImage.Metadata.ExifProfile.Should().BeNull();
        servedImage.Metadata.IptcProfile.Should().BeNull();
        servedImage.Metadata.XmpProfile.Should().BeNull();
        servedImage.Metadata.IccProfile.Should().BeNull();

        // Anonymous cannot call protected user endpoints or read individual lost reports
        (await anonClient.GetAsync("/api/v1/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonClient.GetAsync("/api/v1/lost-reports/mine")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await anonClient.GetAsync($"/api/v1/lost-reports/{createdOpen.Id}")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"privacy-{Guid.NewGuid():N}@campus.edu";
        const string password = "PrivacyPassword1";

        await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Privacy Tester", email, password));
        var login = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
        return client;
    }
}
