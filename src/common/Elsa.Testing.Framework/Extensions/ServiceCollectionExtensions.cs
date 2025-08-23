using Elsa.Testing.Framework.Services;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTestingFramework(this IServiceCollection services)
    {
        services.AddScoped<WorkflowTestScenarioRunner>();
        return services;
    }
}