using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.Features.Abstractions;

/// <summary>
/// Base type for classes that represent a feature. 
/// </summary>
public abstract class FeatureBase : IFeature
{
    /// <summary>
    /// Constructor.
    /// </summary>
    protected FeatureBase(IModule module)
    {
        Module = module;
    }

    /// <summary>
    /// The module this feature is a part of.
    /// </summary>
    public IModule Module { get; }
    
    /// <summary>
    /// A reference to the <see cref="IServiceCollection"/> to which services can be added.
    /// </summary>
    public IServiceCollection Services => Module.Services;

    /// <summary>
    /// Override this method to configure your feature.
    /// </summary>
    public virtual void Configure()
    {
    }

    /// <summary>
    /// Override this method to register any hosted services provided by your feature.
    /// </summary>
    public virtual void ConfigureHostedServices()
    {
    }

    /// <summary>
    /// Override this to register services with <see cref="Services"/>.
    /// </summary>
    public virtual void Apply()
    {
    }

    /// <summary>
    /// Configures the specified hosted service using an optional priority to control in which order it will be registered with the service container.
    /// </summary>
    /// <param name="priority">The priority.</param>
    /// <typeparam name="T">The type of hosted service to configure.</typeparam>
    protected void ConfigureHostedService<T>(int priority = 0) where T : class, IHostedService
    {
        Module.ConfigureHostedService<T>(priority);
    }
}