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
    public static IServiceCollection AddElsa(this IServiceCollection services, Action<IServiceConfiguration>? configure = default)
    {
        services.AddMediator();

        services
            .ConfigureServices(config => config
                .UseWorkflows(workflows =>
                {
                    workflows
                        .UsePersistence()
                        .UseRuntime()
                        .UseManagement();

                    configure?.Invoke(config);
                })
            );

        return services;
    }
}