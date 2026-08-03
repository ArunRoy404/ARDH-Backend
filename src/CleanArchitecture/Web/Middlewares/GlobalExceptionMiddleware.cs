using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Shared.Models.Errors;
using CleanArchitecture.Web.Extensions;

namespace CleanArchitecture.Web.Middlewares;

public class GlobalExceptionMiddleware(ILoggerFactory logger) : IMiddleware
{
    private readonly ILogger _logger = logger.CreateLogger<GlobalExceptionMiddleware>();
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError("GlobalExceptionMiddleware: {exception}", ex);

            // If the response has already been written (e.g. by UseExceptionHandler),
            // there is nothing more we can do.
            if (context.Response.HasStarted)
            {
                return;
            }

            var (statusCode, message, errorCode) = ExceptionResponseMapper.Map(ex);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var apiError = new ApiErrorResponse
            {
                Success = false,
                Message = message,
                Errors = new List<ApiErrorDetail>
                {
                    new(errorCode, message)
                }
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(apiError, options));
        }
    }
}
