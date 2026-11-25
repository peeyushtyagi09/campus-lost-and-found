using System.Net;
using LostAndFound.Api.Middleware;
using LostAndFound.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LostAndFound.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class ExceptionHandlingMiddlewareTests
{
    private readonly ApiFactory _factory;

    public ExceptionHandlingMiddlewareTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ThrownException_ReturnsProblemDetailsWithoutInternalDetails()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/__test/throw");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        body.Should().Contain("An unexpected error occurred.");
        body.Should().NotContain("internal-only-detail");
        body.Should().NotContain("StackTrace");
        body.Should().NotContain("InvalidOperationException");
    }

    [Fact]
    public async Task Middleware_HidesExceptionMessageWhenInvokedDirectly()
    {
        var logger = Substitute.For<ILogger<ExceptionHandlingMiddleware>>();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("internal-only-detail"),
            logger);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/problem+json");
        body.Should().NotContain("internal-only-detail");
        body.Should().NotContain("at ");
        _ = logger.ReceivedCalls().Count();
    }
}
