using Elsa.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace Elsa.Resilience.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering resilience strategy types
/// via <see cref="ResilienceOptions"/>.
/// </summary>
public static class ResilienceServiceCollectionExtensions
{
    /// <summary>
    /// Registers a resilience strategy type with <see cref="ResilienceOptions"/>.
    /// </summary>
    public static IServiceCollection AddResilienceStrategy<T>(this IServiceCollection services) =>
        services.Configure<ResilienceOptions>(options => options.StrategyTypes.Add(typeof(T)));

    /// <summary>
    /// Registers multiple resilience strategy types with <see cref="ResilienceOptions"/>.
    /// </summary>
    public static IServiceCollection AddResilienceStrategies(this IServiceCollection services, IEnumerable<Type> types) =>
        services.Configure<ResilienceOptions>(options =>
        {
            foreach (var type in types)
                options.StrategyTypes.Add(type);
        });
}

