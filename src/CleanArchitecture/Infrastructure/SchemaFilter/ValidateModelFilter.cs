using CleanArchitecture.Application.Common.Exceptions;
using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Shared.Models.Errors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CleanArchitecture.Infrastructure.SchemaFilter;
public class ValidateModelFilter : IActionFilter
{
    public void OnActionExecuted(ActionExecutedContext context)
    {
        // No implementation needed for this filter.
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            // Check for FluentValidation errors first.
            if (context.ModelState.TryGetValue(ApplicationConstants.FluentValidationErrorKey, out var fluentErrorEntry) && fluentErrorEntry.Errors.Any())
            {
                HandleFluentValidationErrors(context, fluentErrorEntry);
                return;
            }

            // Handle general model validation errors.
            HandleModelValidationErrors(context);
        }
    }

    private void HandleFluentValidationErrors(ActionExecutingContext context, ModelStateEntry fluentErrorEntry)
    {
        if (fluentErrorEntry.Errors.First().Exception is ValidationException exception)
        {
            context.Result = new BadRequestObjectResult(exception.ErrorResponse);
        }
    }

    private void HandleModelValidationErrors(ActionExecutingContext context)
    {
        var rawErrors = context.ModelState
            .Where(ms => ms.Value.Errors.Any())
            .SelectMany(ms => ms.Value.Errors.Select(error => new
            {
                Key = ms.Key,
                ErrorMessage = error.ErrorMessage,
                AttemptedValue = ms.Value.AttemptedValue
            }))
            .ToList();

        if (rawErrors.Count > 1)
        {
            rawErrors = rawErrors.Where(e =>
                !(e.ErrorMessage == "The request field is required." || e.ErrorMessage == "A non-empty request body is required.")
            ).ToList();
        }

        var errors = rawErrors
            .Select(errorDetail => new Error(
                $"{ApplicationConstants.Name}.{ErrorRespondCode.BAD_REQUEST}",
                errorDetail.ErrorMessage)
            {
                Property = errorDetail.ErrorMessage ?? "null"
            })
            .ToList();

        context.Result = new BadRequestObjectResult(new ErrorResponse { Errors = errors });
    }
}
