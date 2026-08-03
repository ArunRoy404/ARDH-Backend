using System;
using System.Text.Json;
using System.Collections.Generic;
using CleanArchitecture.Shared.Models.Errors;
using Microsoft.AspNetCore.Diagnostics;

namespace CleanArchitecture.Web.Extensions;
public static class ExceptionMiddlewareExtensions
{
    public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILogger logger)
    {
        app.UseExceptionHandler(new ExceptionHandlerOptions
        {
            AllowStatusCode404Response = true,
            ExceptionHandler = async context =>
            {
                // If the response has already started, we cannot safely write a new one.
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.ContentType = "application/json";

                var errorId = Guid.NewGuid();

                var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                if (contextFeature != null)
                {
                    var (statusCode, errorMessage, errorCode) = ExceptionResponseMapper.Map(contextFeature.Error);

                    context.Response.StatusCode = statusCode;

                    var apiError = new ApiErrorResponse
                    {
                        Success = false,
                        Message = errorMessage,
                        Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail(errorCode, errorMessage)
                        }
                    };

                    var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(apiError, options));
                    logger.LogError("ErrorId:{errorId} Exception:{contextFeature.Error}", errorId, contextFeature.Error);
                }
            }
        });
    }
}
