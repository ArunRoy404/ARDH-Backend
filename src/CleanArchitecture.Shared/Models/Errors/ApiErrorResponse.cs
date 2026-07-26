using System.Collections.Generic;

namespace CleanArchitecture.Shared.Models.Errors;

public class ApiErrorResponse
{
    public bool Success { get; set; } = false;
    public string Message { get; set; } = "An error occurred.";
    public List<ApiErrorDetail> Errors { get; set; } = new();

    public ApiErrorResponse() { }

    public ApiErrorResponse(string message)
    {
        Message = message;
    }

    public ApiErrorResponse(string message, List<ApiErrorDetail> errors)
    {
        Message = message;
        Errors = errors;
    }
}

public class ApiErrorDetail
{
    public string? Code { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Property { get; set; }

    public ApiErrorDetail() { }

    public ApiErrorDetail(string? code, string message, string? property = null)
    {
        Code = code;
        Message = message;
        Property = property;
    }
}
