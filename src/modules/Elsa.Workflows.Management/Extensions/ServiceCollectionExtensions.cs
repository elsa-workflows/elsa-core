using Elsa.Workflows.Management.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Management.Extensions;

/// <summary>
/// Provides extension methods to <see cref="IServiceCollection"/> for various kinds of services and providers. 
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="IActivityProvider"/>.
    /// </summary>
    public static IServiceCollection AddActivityProvider<T>(this IServiceCollection services) where T : class, IActivityProvider => services.AddSingleton<IActivityProvider, T>();
}