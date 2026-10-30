using Elsa.Logging.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Logging.Extensions;

/// <summary>
/// Provides extension methods for registering log sink implementations within the dependency injection system.
/// </summary>
public static class LogSinkServiceCollectionExtensions
{
    /// <summary>
    /// Registers a log sink implementation of type <typeparamref name="T"/> within the dependency injection system.
    /// </summary>
    /// <typeparam name="T">The type of the log sink to register. Must implement <see cref="ILogSink"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the log sink to.</param>
    public static IServiceCollection AddLogSink<T>(this IServiceCollection services) where T : class, ILogSink
    {
        return services.AddSingleton<ILogSink, T>();
    }
}