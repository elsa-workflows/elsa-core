using System;
using Elsa.Mediator.Extensions;
using Elsa.ServiceConfiguration.Extensions;
using Elsa.ServiceConfiguration.Services;
using Elsa.Workflows.Core;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddElsa(this IServiceCollection services, Action<IServiceConfiguration> configure)
    {
        var serviceConfiguration = services.AddElsa();
        configure(serviceConfiguration);
        serviceConfiguration.Apply();
        return services;
    }


    public static IServiceConfiguration AddElsa(this IServiceCollection services)
    {
        services.AddMediator();

        var serviceConfiguration = services
            .ConfigureServices()
            .UseWorkflows()
            .UsePersistence()
            .UseRuntime()
            .UseManagement();

        return serviceConfiguration;
    }
}