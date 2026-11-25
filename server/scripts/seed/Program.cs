using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
if (string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Refusing to run dev seed script when ASPNETCORE_ENVIRONMENT=Production.");
    return 1;
}

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
    .AddEnvironmentVariables()
    .Build();

var mongoConnectionString = configuration["Mongo:ConnectionString"];
if (string.IsNullOrWhiteSpace(mongoConnectionString))
{
    mongoConnectionString = "mongodb://localhost:27017";
}

var mongoDatabaseName = configuration["Mongo:DatabaseName"];
if (string.IsNullOrWhiteSpace(mongoDatabaseName))
{
    mongoDatabaseName = "lostandfound";
}

var encryptionKey = configuration["Encryption:Key"];
if (string.IsNullOrWhiteSpace(encryptionKey))
{
    encryptionKey = "MDEyMzQ1Njc4OWFiY2RlZjAxMjM0NTY3ODlhYmNkZWY=";
}

var uploadPath = configuration["Storage:UploadPath"];
if (string.IsNullOrWhiteSpace(uploadPath))
{
    uploadPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "server", "src", "LostAndFound.Api", "uploads"));
}

var mongoOptions = Options.Create(new MongoOptions
{
    ConnectionString = mongoConnectionString,
    DatabaseName = mongoDatabaseName
});
var encryptionOptions = Options.Create(new EncryptionOptions { Key = encryptionKey });
var storageOptions = Options.Create(new StorageOptions
{
    UploadPath = uploadPath,
    MaxPhotoBytes = StorageOptions.DefaultMaxPhotoBytes
});

using var client = new MongoClient(mongoConnectionString);
var context = new MongoContext(client, mongoOptions);
var indexService = new MongoIndexHostedService(context, NullLogger<MongoIndexHostedService>.Instance);
await indexService.StartAsync(CancellationToken.None);

var encryption = new AesGcmEncryptionService(encryptionOptions);
var emailHasher = new HmacEmailHasher(encryptionOptions);
var passwordHasher = new PasswordHasher<User>();
var userRepo = new MongoUserRepository(context);
var reportRepo = new MongoLostReportRepository(context);
var sanitizer = new ImageSharpPhotoSanitizer(storageOptions);
var fileStorage = new LocalFileStorage(storageOptions);

var seededUsers = new (string DisplayName, string Email, string Password, UserRole Role)[]
{
    ("Campus Admin", "admin@campus.edu", "AdminPass123!", UserRole.Admin),
    ("Desk Staff", "staff@campus.edu", "StaffPass123!", UserRole.Staff),
    ("Alice", "alice@campus.edu", "StudentPass1!", UserRole.User),
    ("Bob", "bob@campus.edu", "StudentPass1!", UserRole.User),
    ("Carol", "carol@campus.edu", "StudentPass1!", UserRole.User)
};

var userIds = new List<ObjectId>();
foreach (var spec in seededUsers)
{
    var normalized = emailHasher.Normalize(spec.Email);
    var hash = emailHasher.Hash(normalized);
    var existing = await userRepo.GetByEmailHashAsync(hash, CancellationToken.None);
    if (existing is not null)
    {
        userIds.Add(existing.Id);
        continue;
    }

    var user = new User
    {
        Id = ObjectId.GenerateNewId(),
        DisplayName = spec.DisplayName,
        Email = encryption.Encrypt(normalized),
        EmailHash = hash,
        Role = spec.Role,
        CreatedAt = DateTime.UtcNow
    };
    user.PasswordHash = passwordHasher.HashPassword(user, spec.Password);
    await userRepo.CreateAsync(user, CancellationToken.None);
    userIds.Add(user.Id);
}

var reporterIds = userIds.Skip(2).ToArray();
var existingReportsCount = await context.LostReports.CountDocumentsAsync(Builders<LostReport>.Filter.Empty);
if (existingReportsCount >= 20)
{
    Console.WriteLine($"Seed skipped lost reports: {existingReportsCount} already present.");
    return 0;
}

var samplePhotos = new List<string>();
var colors = new[] { Color.CornflowerBlue, Color.ForestGreen, Color.IndianRed };
foreach (var color in colors)
{
    using var img = new Image<Rgba32>(200, 150, color);
    await using var ms = new MemoryStream();
    await img.SaveAsJpegAsync(ms);
    ms.Position = 0;
    var sanitized = await sanitizer.SanitizeAsync(ms, CancellationToken.None);
    if (sanitized.IsSuccess && sanitized.Value is not null)
    {
        samplePhotos.Add(await fileStorage.SaveAsync(sanitized.Value.Content, sanitized.Value.Extension, CancellationToken.None));
    }
}

var categories = Enum.GetValues<ItemCategory>();
var areas = new[] { "Library", "Student Union", "Hostel Block A", "Engineering Block", "Main Ground", "Cafeteria" };

for (var i = 0; i < 20; i++)
{
    var title = $"Sample Lost Item #{i + 1:00} - {categories[i % categories.Length]}";
    var alreadyExists = await context.LostReports
        .Find(r => r.Title == title)
        .AnyAsync();
    if (alreadyExists)
    {
        continue;
    }

    var now = DateTime.UtcNow.AddHours(-i);
    var report = new LostReport
    {
        Id = ObjectId.GenerateNewId(),
        ReporterId = reporterIds[i % reporterIds.Length],
        Title = title,
        Category = categories[i % categories.Length],
        PublicDescription = $"Lost near {areas[i % areas.Length]}. Please contact campus lost and found desk if seen.",
        CampusArea = areas[i % areas.Length],
        DateLost = DateTime.UtcNow.Date.AddDays(-(i % 10)),
        PhotoFileName = i < 6 && samplePhotos.Count > 0 ? samplePhotos[i % samplePhotos.Count] : null,
        Status = i == 19 ? ReportStatus.Matched : ReportStatus.Open,
        PrivateDetails = encryption.Encrypt($"Fake private serial SN-{1000 + i} and scratch mark on bottom."),
        ContactPhone = encryption.Encrypt($"555-01{i:00}-99"),
        ContactEmail = encryption.Encrypt($"reporter{i + 1}@campus.edu"),
        CreatedAt = now,
        UpdatedAt = now
    };

    await reportRepo.CreateAsync(report, CancellationToken.None);
}

Console.WriteLine("Seed complete: 1 admin, 1 staff, 3 users, 20 sample lost reports, 3 sanitized photos.");
return 0;
