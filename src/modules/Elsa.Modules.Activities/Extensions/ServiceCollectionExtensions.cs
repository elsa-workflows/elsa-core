using Elsa.Activities.Configurators;
using Elsa.Activities.Resolvers;
using Elsa.Contracts;
using Elsa.Management.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services for activities provided by this package.
    /// </summary>
    public static IServiceCollection AddActivityServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IActivityNodeResolver, SwitchActivityNodeResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>();
    }
}