using Elsa.Http.OpenApi.Contracts;
using Elsa.Http.OpenApi.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.OpenApi.Extensions;

/// <summary>
/// Extension methods for configuring HTTP OpenAPI services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds HTTP OpenAPI services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddElsaHttpOpenApi(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowEndpointExtractor, WorkflowEndpointExtractor>();
        services.AddSingleton<IElsaVersionProvider, ElsaVersionProvider>();
        services.AddSingleton<IOpenApiGenerator, OpenApiGenerator>();

        return services;
    }
}
