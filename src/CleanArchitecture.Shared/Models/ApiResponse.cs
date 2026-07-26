namespace CleanArchitecture.Shared.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public string Message { get; set; } = "Request processed successfully.";
    public T? Data { get; set; }

    public ApiResponse() { }

    public ApiResponse(T data, string message = "Request processed successfully.")
    {
        Data = data;
        Message = message;
    }
}
