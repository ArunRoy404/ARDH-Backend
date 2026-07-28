
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CleanArchitecture.Shared.Models.Errors;

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
            
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var apiError = new ApiErrorResponse
            {
                Success = false,
                Message = "An unexpected error occurred.",
                Errors = new List<ApiErrorDetail>
                {
                    new ApiErrorDetail("Ardh.InternalError", ex.Message)
                }
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(apiError, options));
        }
    }
}
