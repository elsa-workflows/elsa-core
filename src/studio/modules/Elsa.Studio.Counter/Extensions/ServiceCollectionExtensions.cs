using Elsa.Studio.Contracts;
using Elsa.Studio.Counter.Menu;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Counter.Extensions;

/// Extension methods to add the Counter module to the service collection.
public static class ServiceCollectionExtensions
{
    /// Adds the Counter module to the service collection.
    public static IServiceCollection AddCounterModule(this IServiceCollection services)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, CounterMenu>();
    }
}