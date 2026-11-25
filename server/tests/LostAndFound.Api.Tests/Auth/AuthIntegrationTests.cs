using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LostAndFound.Api.Dtos.Auth;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Privacy;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace LostAndFound.Api.Tests.Auth;

[Collection(ApiCollection.Name)]
public sealed class AuthIntegrationTests
{
    private readonly ApiFactory _factory;

    public AuthIntegrationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_HappyPath_ReturnsCreatedWithoutPasswordHash()
    {
        using var client = _factory.CreateClient();
        var email = $"alice-{Guid.NewGuid():N}@campus.edu";

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Alice Student", email, "CampusPass123"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rawBody = await response.Content.ReadAsStringAsync();
        AssertNoPasswordHashLeak(rawBody);

        var payload = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        payload.Should().NotBeNull();
        payload!.User.DisplayName.Should().Be("Alice Student");
        payload.User.Email.Should().Be(email);
        payload.User.Role.Should().Be("User");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsNeutralConflict()
    {
        using var client = _factory.CreateClient();
        var email = $"dup-{Guid.NewGuid():N}@campus.edu";

        var first = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("First User", email, "CampusPass123"));
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Second User", email.ToUpperInvariant(), "CampusPass123"));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await second.Content.ReadAsStringAsync();
        body.Should().NotContainEquivalentOf("already exists");
        body.Should().NotContain(email);
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase123")]
    [InlineData("ALLUPPERCASE123")]
    [InlineData("NoDigitsHereAtAll")]
    public async Task Register_WeakPassword_ReturnsBadRequest(string weakPassword)
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Weak Pass", $"weak-{Guid.NewGuid():N}@campus.edu", weakPassword));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Login_SuccessAndProtectedMeRoute_ReturnsTokenAndUserWithoutPasswordHash()
    {
        using var client = _factory.CreateClient();
        var email = $"bob-{Guid.NewGuid():N}@campus.edu";
        const string password = "StrongPassword99";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Bob Staff", email, password));
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var unauthorizedMe = await client.GetAsync("/api/v1/auth/me");
        unauthorizedMe.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(email, password));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        AssertNoPasswordHashLeak(loginBody);

        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        loginPayload.Should().NotBeNull();
        loginPayload!.AccessToken.Should().NotBeNullOrWhiteSpace();
        loginPayload.ExpiresAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(50));
        loginPayload.User.Email.Should().Be(email);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(loginPayload.AccessToken);
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == loginPayload.User.Id);
        jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == "User");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meBody = await meResponse.Content.ReadAsStringAsync();
        AssertNoPasswordHashLeak(meBody);
        var mePayload = await meResponse.Content.ReadFromJsonAsync<UserDto>();
        mePayload!.Id.Should().Be(loginPayload.User.Id);
        mePayload.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_FiveFailures_LocksAccountAndResetsOnValidLoginBeforeFive()
    {
        using var client = _factory.CreateClient();
        var email = $"lockout-{Guid.NewGuid():N}@campus.edu";
        const string password = "LockoutPassword1";

        await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterRequest("Lockout User", email, password));

        // 2 wrong attempts, then 1 good attempt resets counter
        for (var i = 0; i < 2; i++)
        {
            var fail = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPass123"));
            fail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var resetLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        resetLogin.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now 5 consecutive failures lock out the account
        for (var i = 0; i < 5; i++)
        {
            var fail = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "WrongPass123"));
            fail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        var lockedAttempt = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        lockedAttempt.StatusCode.Should().Be(HttpStatusCode.Locked);
    }

    [Fact]
    public async Task RolePolicies_EnforceStaffAndAdminAccess()
    {
        var staffEmail = $"staff-{Guid.NewGuid():N}@campus.edu";
        var adminEmail = $"admin-{Guid.NewGuid():N}@campus.edu";
        const string password = "RolePassword123";

        await SeedUserWithRoleAsync("Staff Member", staffEmail, password, UserRole.Staff);
        await SeedUserWithRoleAsync("Admin Member", adminEmail, password, UserRole.Admin);

        using var userClient = _factory.CreateClient();
        var userEmail = $"student-{Guid.NewGuid():N}@campus.edu";
        await userClient.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest("Student", userEmail, password));
        var userLogin = await (await userClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(userEmail, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userLogin!.AccessToken);

        (await userClient.GetAsync("/__test/staff")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await userClient.GetAsync("/__test/admin")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var staffClient = _factory.CreateClient();
        var staffLogin = await (await staffClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(staffEmail, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        staffClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffLogin!.AccessToken);

        (await staffClient.GetAsync("/__test/staff")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await staffClient.GetAsync("/__test/admin")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var adminClient = _factory.CreateClient();
        var adminLogin = await (await adminClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(adminEmail, password)))
            .Content.ReadFromJsonAsync<LoginResponse>();
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminLogin!.AccessToken);

        (await adminClient.GetAsync("/__test/admin")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SeedUserWithRoleAsync(string displayName, string email, string password, UserRole role)
    {
        using var scope = _factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var encryption = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
        var emailHasher = scope.ServiceProvider.GetRequiredService<IEmailHasher>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

        var normalized = emailHasher.Normalize(email);
        var user = new User
        {
            Id = ObjectId.GenerateNewId(),
            DisplayName = displayName,
            Email = encryption.Encrypt(normalized),
            EmailHash = emailHasher.Hash(normalized),
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, password);
        await users.CreateAsync(user, CancellationToken.None);
    }

    private static void AssertNoPasswordHashLeak(string json)
    {
        json.Should().NotContainEquivalentOf("passwordHash");
        json.Should().NotContainEquivalentOf("emailHash");
        json.Should().NotContainEquivalentOf("failedLoginCount");
        json.Should().NotContainEquivalentOf("lockoutEnd");
    }
}
