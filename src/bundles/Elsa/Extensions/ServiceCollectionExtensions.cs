using System;
using Elsa.Mediator.Extensions;
using Elsa.Options;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddElsa(this IServiceCollection services, Action<ElsaOptionsConfigurator>? configure = default)
    {
        services.AddMediator();

        return services
            .AddElsaCore(elsa =>
            {
                elsa.ConfigureElsaRuntime();
                elsa.AddElsaManagement();
                configure?.Invoke(elsa);
            });
    }
}