# ==============================================================================
# Multi-stage Dockerfile for TaskManagement Web API (.NET 10)
# Compatible with Vercel Container Runtime, Railway, Render, and Local Docker
# ==============================================================================

# ------------------------------------------------------------------------------
# 1. Build Stage
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy MSBuild and Directory Package management props first for caching
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Copy project files for dependency caching
COPY ["src/TaskManagement.Domain/TaskManagement.Domain.csproj", "src/TaskManagement.Domain/"]
COPY ["src/TaskManagement.Application/TaskManagement.Application.csproj", "src/TaskManagement.Application/"]
COPY ["src/TaskManagement.Infrastructure/TaskManagement.Infrastructure.csproj", "src/TaskManagement.Infrastructure/"]
COPY ["src/TaskManagement.API/TaskManagement.API.csproj", "src/TaskManagement.API/"]

# Restore dependencies
RUN dotnet restore "src/TaskManagement.API/TaskManagement.API.csproj"

# Copy all source files
COPY src/ src/

# Publish the Release artifact
RUN dotnet publish "src/TaskManagement.API/TaskManagement.API.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# 2. Runtime Stage
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Expose ports for standard .NET (8080) and web (80)
EXPOSE 8080
EXPOSE 80

# Configure production environment defaults
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_RUNNING_IN_CONTAINER=true

# Copy compiled application
COPY --from=build /app/publish .

# Run as non-root user (UID 1654 built into .NET container images)
USER app

# Dynamically listen on Vercel's $PORT or fallback to 8080
ENTRYPOINT ["sh", "-c", "dotnet TaskManagement.API.dll --urls http://+:${PORT:-8080}"]
