using Elsa.Testing.Framework.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Testing.Framework.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsaTestingFramework(this IServiceCollection services)
    {
        services.AddScoped<WorkflowTestScenarioRunner>();
        return services;
    }
}