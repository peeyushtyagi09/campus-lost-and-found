using System.Net;
using System.Net.Http.Json;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LostAndFound.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class HealthEndpointTests
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Live_ReturnsHealthyJson()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        response.Headers.Should().ContainKey("X-Content-Type-Options");

        var body = await response.Content.ReadFromJsonAsync<HealthPayload>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().Contain(check => check.Name == "self" && check.Status == "Healthy");
    }

    [Fact]
    public async Task Ready_ReturnsHealthyWhenMongoIsReachable()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var healthCheckService = _factory.Services.GetRequiredService<HealthCheckService>();
            var report = await healthCheckService.CheckHealthAsync(check => check.Tags.Contains("ready"));
            var details = string.Join("; ", report.Entries.Select(e => $"{e.Key}: {e.Value.Status} ({e.Value.Description}) {e.Value.Exception}"));
            response.StatusCode.Should().Be(HttpStatusCode.OK, because: details);
        }

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthPayload>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Healthy");
        body.Checks.Should().Contain(check => check.Name == "mongodb" && check.Status == "Healthy");
    }

    private sealed record HealthPayload(string Status, IReadOnlyList<HealthCheckPayload> Checks);

    private sealed record HealthCheckPayload(string Name, string Status);
}
