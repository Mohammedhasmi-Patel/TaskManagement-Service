using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Application.Interfaces;
using TaskManagement.Application.Services;

namespace TaskManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICompaniesService, CompaniesService>();
        services.AddScoped<ITechnologiesService, TechnologiesService>();
        services.AddScoped<IProjectsService, ProjectsService>();
        services.AddScoped<IStorageService, StorageService>();

        return services;
    }
}
