using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Elsa.Studio.DomInterop.Extensions;

/// <summary>
/// Provides extension methods for configuring DOM interop services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds DOM interop services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDomInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IDomAccessor, DomJsInterop>();

        return services;
    }

    /// <summary>
    /// Adds clipboard interop services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddClipboardInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IClipboard, ClipboardJsInterop>();

        return services;
    }
    
    /// <summary>
    /// Adds file download interop services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddDownloadInterop(this IServiceCollection services)
    {
        services.TryAddScoped<IFiles, FilesJsInterop>();

        return services;
    }
}