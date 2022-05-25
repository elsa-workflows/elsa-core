using System;
using Elsa.Mediator.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Options;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddElsa(this IServiceCollection services, Action<ElsaOptionsConfigurator>? configure = default)
    {
        services.AddMediator();

        return services
            .AddWorkflowCore(elsa =>
            {
                elsa.ConfigureWorkflowRuntime();
                elsa.ConfigureWorkflowPersistence();
                elsa.AddWorkflowManagement();
                configure?.Invoke(elsa);
            });
    }
}