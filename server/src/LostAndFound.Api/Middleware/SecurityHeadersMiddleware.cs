using Microsoft.Net.Http.Headers;

namespace LostAndFound.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(AddHeaders, context);
        return _next(context);
    }

    private Task AddHeaders(object state)
    {
        var context = (HttpContext)state;
        var headers = context.Response.Headers;
        headers[HeaderNames.XContentTypeOptions] = "nosniff";
        headers[HeaderNames.XFrameOptions] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'";

        if (_environment.IsProduction() && context.Request.IsHttps)
        {
            headers[HeaderNames.StrictTransportSecurity] = "max-age=31536000; includeSubDomains";
        }

        return Task.CompletedTask;
    }
}
