using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Astriology.API.Filters;

/// <summary>
/// Runs the FluentValidation validator for every action argument that has one,
/// before the action body executes.
/// </summary>
/// <remarks>
/// Registered globally so validation cannot be forgotten on a new endpoint: adding a
/// validator for a request type is enough to have it enforced. Failures come back as
/// RFC 7807 validation problem details, the same shape the rest of the API uses.
/// </remarks>
public sealed class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _serviceProvider;

    public ValidationFilter(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var errors = new Dictionary<string, string[]>();

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

            if (result.IsValid)
            {
                continue;
            }

            foreach (var group in result.Errors.GroupBy(failure => failure.PropertyName))
            {
                errors[group.Key] = [.. group.Select(failure => failure.ErrorMessage)];
            }
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(errors)
            {
                Title = "Geçersiz istek",
                Status = StatusCodes.Status400BadRequest,
            });

            return;
        }

        await next();
    }
}
