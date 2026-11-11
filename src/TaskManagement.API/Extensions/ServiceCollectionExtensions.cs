using Microsoft.AspNetCore.Mvc;
using TaskManagement.API.Middleware;
using TaskManagement.Application.Common.Models;

namespace TaskManagement.API.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers presentation and API layer services including controllers, exception handling,
    /// problem details, Swagger documentation, and CORS.
    /// </summary>
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var firstError = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => !string.IsNullOrWhiteSpace(e.ErrorMessage) ? e.ErrorMessage : e.Exception?.Message)
                    .FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
                    ?? "One or more validation failures have occurred.";

                var response = ApiResponse<object?>.FailureResponse(firstError, StatusCodes.Status400BadRequest);
                return new BadRequestObjectResult(response);
            };
        });
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        services.AddSwaggerDocumentation();
        services.AddCustomCors(configuration);

        return services;
    }
}
