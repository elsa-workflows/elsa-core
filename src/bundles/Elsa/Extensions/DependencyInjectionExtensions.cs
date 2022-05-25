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

        return services
            .ConfigureServices(config => config
                .ConfigureWorkflows(workflows =>
                {
                    workflows
                        .ConfigureRuntime()
                        .ConfigurePersistence()
                        .ConfigureWorkflowManagement();

                    configure?.Invoke(config);
                })
            );
    }
}