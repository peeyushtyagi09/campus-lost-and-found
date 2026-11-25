using Microsoft.AspNetCore.Mvc;

namespace LostAndFound.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (BadHttpRequestException badRequest)
        {
            var statusCode = badRequest.StatusCode is StatusCodes.Status413PayloadTooLarge
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
            var title = statusCode == StatusCodes.Status413PayloadTooLarge
                ? "Request payload exceeds maximum allowed size."
                : "Malformed request payload.";

            _logger.LogWarning("Rejected malformed or oversized request {Method} {Path} with {StatusCode}.",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode);

            await WriteProblemAsync(context, statusCode, title);
        }
        catch (InvalidDataException invalidData)
        {
            var statusCode = invalidData.Message.Contains("length limit", StringComparison.OrdinalIgnoreCase)
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
            var title = statusCode == StatusCodes.Status413PayloadTooLarge
                ? "Request payload exceeds maximum allowed size."
                : "Malformed request payload.";

            _logger.LogWarning("Rejected invalid request data {Method} {Path} with {StatusCode}.",
                context.Request.Method,
                context.Request.Path.Value,
                statusCode);

            await WriteProblemAsync(context, statusCode, title);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}",
                context.Request.Method,
                context.Request.Path.Value);

            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.");
        }
    }

    private static Task WriteProblemAsync(HttpContext context, int statusCode, string title)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = "https://tools.ietf.org/html/rfc7807",
            Instance = context.Request.Path
        };

        return context.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json");
    }
}
