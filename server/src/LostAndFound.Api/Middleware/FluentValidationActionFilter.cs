using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace LostAndFound.Api.Middleware;

public sealed class FluentValidationActionFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public FluentValidationActionFilter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (!result.IsValid)
            {
                foreach (var failure in result.Errors)
                {
                    var key = string.IsNullOrEmpty(failure.PropertyName)
                        ? string.Empty
                        : char.ToLowerInvariant(failure.PropertyName[0]) + failure.PropertyName[1..];

                    if (errors.TryGetValue(key, out var existing))
                    {
                        errors[key] = [.. existing, failure.ErrorMessage];
                    }
                    else
                    {
                        errors[key] = [failure.ErrorMessage];
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            var problem = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc7807",
                Instance = context.HttpContext.Request.Path
            };

            context.Result = new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
            return;
        }

        await next();
    }
}
