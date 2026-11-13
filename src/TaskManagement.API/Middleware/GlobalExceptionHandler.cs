using Microsoft.AspNetCore.Diagnostics;
using TaskManagement.Application.Common.Exceptions;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.API.Middleware;

public class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IHostEnvironment environment) : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger = logger;
    private readonly IHostEnvironment _environment = environment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, message) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                GetFirstValidationError(validationException)),
            BadRequestException badRequestException => (
                StatusCodes.Status400BadRequest,
                badRequestException.Message),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                notFoundException.Message),
            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                conflictException.Message),
            UnauthorizedException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                unauthorizedException.Message),
            UnauthorizedAccessException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                unauthorizedException.Message),
            _ => HandleUnhandledException(exception)
        };

        var response = ApiResponse<object?>.FailureResponse(message, statusCode);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(response, cancellationToken);

        return true;
    }

    private static string GetFirstValidationError(ValidationException ex)
    {
        return ex.Errors
            .SelectMany(e => e.Value)
            .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
            ?? ex.Message;
    }

    private (int StatusCode, string Message) HandleUnhandledException(Exception ex)
    {
        _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);

        // var message = _environment.IsDevelopment()
        //     ? ex.Message
        //     : "An unexpected error occurred. Please try again later.";
        var message =  ex.Message;
        return (StatusCodes.Status500InternalServerError, message);
    }
}
