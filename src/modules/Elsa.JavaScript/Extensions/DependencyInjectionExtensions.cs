using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds a <see cref="IFunctionDefinitionProvider"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The type of the function definition provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddFunctionDefinitionProvider<T>(this IServiceCollection services) where T: class, IFunctionDefinitionProvider => services.AddSingleton<IFunctionDefinitionProvider, T>();

    /// <summary>
    /// Adds a <see cref="IFunctionDefinitionProvider"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">A callback to create the function definition provider.</param>
    /// <typeparam name="T">The type of the function definition provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddFunctionDefinitionProvider<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T: class, IFunctionDefinitionProvider => 
        services.AddSingleton<IFunctionDefinitionProvider, T>(factory);
    
    /// <summary>
    /// Adds a <see cref="ITypeDefinitionProvider"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="T">The type of the type definition provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTypeDefinitionProvider<T>(this IServiceCollection services) where T: class, ITypeDefinitionProvider => services.AddSingleton<ITypeDefinitionProvider, T>();

    /// <summary>
    /// Adds a <see cref="ITypeDefinitionProvider"/> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="factory">A callback to create the type definition provider.</param>
    /// <typeparam name="T">The type of the type definition provider.</typeparam>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTypeDefinitionProvider<T>(this IServiceCollection services, Func<IServiceProvider, T> factory) where T: class, ITypeDefinitionProvider => 
        services.AddSingleton<ITypeDefinitionProvider, T>();
}