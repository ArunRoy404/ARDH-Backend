using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CleanArchitecture.Shared.Models;
using CleanArchitecture.Shared.Models.Errors;

namespace CleanArchitecture.Web.Filters;

public class ResponseWrapperFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            var statusCode = objectResult.StatusCode ?? 200;

            if (statusCode >= 200 && statusCode < 300)
            {
                var value = objectResult.Value;
                var valueType = value?.GetType();
                var isAlreadyWrapped = valueType != null && valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>);

                if (!isAlreadyWrapped)
                {
                    var wrappedValue = new ApiResponse<object>
                    {
                        Success = true,
                        Message = "Request processed successfully.",
                        Data = value
                    };

                    objectResult.Value = wrappedValue;
                }
            }
            else
            {
                var value = objectResult.Value;

                if (value is not ApiErrorResponse)
                {
                    var apiError = new ApiErrorResponse
                    {
                        Success = false,
                        Message = "An error occurred while processing the request."
                    };

                    if (value is string message)
                    {
                        apiError.Message = message;
                        apiError.Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail(null, message)
                        };
                    }
                    else if (value is ErrorResponse errorResponse)
                    {
                        apiError.Message = "Validation failed.";
                        apiError.Errors = errorResponse.Errors.Select(e => new ApiErrorDetail
                        {
                            Code = e.Code,
                            Message = e.Message,
                            Property = e.Property
                        }).ToList();
                    }
                    else if (value is Error sharedError)
                    {
                        apiError.Message = sharedError.Message;
                        apiError.Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail
                            {
                                Code = sharedError.Code,
                                Message = sharedError.Message,
                                Property = sharedError.Property
                            }
                        };
                    }
                    else if (value is CleanArchitecture.Domain.Error domainError)
                    {
                        apiError.Message = domainError.Message;
                        apiError.Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail
                            {
                                Code = domainError.Code,
                                Message = domainError.Message
                            }
                        };
                    }
                    else if (value is ProblemDetails problemDetails)
                    {
                        apiError.Message = problemDetails.Detail ?? problemDetails.Title ?? "An error occurred.";
                        apiError.Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail(problemDetails.Type, apiError.Message)
                        };
                    }
                    else
                    {
                        var strVal = value?.ToString() ?? "An error occurred.";
                        apiError.Message = strVal;
                        apiError.Errors = new List<ApiErrorDetail>
                        {
                            new ApiErrorDetail(null, strVal)
                        };
                    }

                    objectResult.Value = apiError;
                }
            }
        }
        else if (context.Result is StatusCodeResult statusCodeResult)
        {
            var statusCode = statusCodeResult.StatusCode;
            if (statusCode >= 200 && statusCode < 300)
            {
                context.Result = new ObjectResult(new ApiResponse<object>
                {
                    Success = true,
                    Message = "Request processed successfully.",
                    Data = null
                })
                {
                    StatusCode = statusCode
                };
            }
            else
            {
                context.Result = new ObjectResult(new ApiErrorResponse
                {
                    Success = false,
                    Message = $"Action failed with status code {statusCode}."
                })
                {
                    StatusCode = statusCode
                };
            }
        }

        await next();
    }
}
