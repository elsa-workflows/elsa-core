using Elsa.Contracts;
using Elsa.Modules.Activities.Configurators;
using Elsa.Modules.Activities.Contracts;
using Elsa.Modules.Activities.Providers;
using Elsa.Modules.Activities.Resolvers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Modules.Activities.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers required services for activities provided by this package.
    /// </summary>
    public static IServiceCollection AddActivityServices(this IServiceCollection services)
    {
        return services
            .AddSingleton<IActivityNodeResolver, SwitchActivityNodeResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()
            .AddSingleton<IStandardInStreamProvider>(new StandardInStreamProvider(Console.In))
            .AddSingleton<IStandardOutStreamProvider>(new StandardOutStreamProvider(Console.Out))
            ;
    }
}