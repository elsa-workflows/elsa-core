using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Decorates an API client with a Blazor service accessor, ensuring that the service provider is available to the API client when calling DI-resolved delegating handlers.
/// </summary>
public class BlazorScopedProxyApi<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T> : DispatchProxy
{
    private T _decoratedApi = default!;
    private IBlazorServiceAccessor _blazorServiceAccessor = null!;
    private IServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Initializes the BlazorScopedProxyApi instance with the specified parameters.
    /// </summary>
    /// <param name="decoratedApi">The API client instance to be decorated.</param>
    /// <param name="blazorServiceAccessor">The Blazor service accessor instance for setting the scoped service provider.</param>
    /// <param name="serviceProvider">The service provider for dependency injection.</param>
    public void Initialize(T decoratedApi, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider)
    {
        _decoratedApi = decoratedApi;
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        _blazorServiceAccessor.Services = _serviceProvider;
        return targetMethod?.Invoke(_decoratedApi, args);
    }
}