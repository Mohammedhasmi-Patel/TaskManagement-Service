namespace TaskManagement.API.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "AppCorsPolicy";

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Direct environment variable or scalar configuration takes precedence
        var rawConfigValue = configuration["Cors:AllowedOrigins"]
                             ?? configuration["Cors:allow"]
                             ?? configuration["Cors:AllowedOrigin"]
                             ?? configuration["Cors__AllowedOrigins"]
                             ?? configuration["Cors__allow"]
                             ?? configuration["CORS_ALLOWED_ORIGINS"];

        string[]? allowedOrigins = null;

        if (!string.IsNullOrWhiteSpace(rawConfigValue))
        {
            allowedOrigins = rawConfigValue.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        else
        {
            // 2. Fallback to array section from appsettings.json
            allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        }

        allowedOrigins ??= ["http://localhost:3000", "http://localhost:5173"];

        // 3. Normalize: remove whitespace and strip trailing slashes (e.g. "https://app.vercel.app/" -> "https://app.vercel.app")
        var normalizedOrigins = allowedOrigins
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var allowAnyOrigin = normalizedOrigins.Length == 0
                             || normalizedOrigins.Any(origin => origin == "*");

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                if (allowAnyOrigin)
                {
                    // AllowAnyOrigin with credentials support via origin reflector
                    builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
                else
                {
                    builder.WithOrigins(normalizedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
    {
        return app.UseCors(PolicyName);
    }
}
