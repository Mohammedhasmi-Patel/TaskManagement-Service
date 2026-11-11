namespace TaskManagement.Application.Common.Models;

public record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data,
    int StatusCode)
{
    public static ApiResponse<T> SuccessResponse(T data, string message = "Request completed successfully.", int statusCode = 200) =>
        new(true, message, data, statusCode);

    public static ApiResponse<T> FailureResponse(string message, int statusCode) =>
        new(false, message, default, statusCode);
}

