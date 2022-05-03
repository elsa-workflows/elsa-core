using System;
using Elsa.Management.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Modules.Activities.Extensions;
using Elsa.Options;
using Elsa.Persistence.Extensions;
using Elsa.Persistence.InMemory.Extensions;
using Elsa.Runtime.Extensions;
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
                elsa.UsePersistence(persistence => persistence.UseInMemoryProvider());
                elsa.UseActivityServices();
                elsa.AddElsaRuntime();
                elsa.AddElsaManagement();
                configure?.Invoke(elsa);
            });
    }
}