using System.Reflection;
using Elsa.Features.Attributes;
using Elsa.Features.Extensions;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Features.Implementations;

public class Module : IModule
{
    private record HostedServiceDescriptor(int Order, Type HostedServiceType);

    private readonly ISet<IFeature> _features = new HashSet<IFeature>();
    private readonly ICollection<HostedServiceDescriptor> _hostedServiceDescriptors = new List<HostedServiceDescriptor>();

    public Module(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public T Configure<T>(Action<T>? configure = default) where T : class, IFeature => Configure(module => (T)Activator.CreateInstance(typeof(T), module)!, configure);

    public T Configure<T>(Func<IModule, T> factory, Action<T>? configure = default) where T : class, IFeature
    {
        if (_features.FirstOrDefault(x => x is T) is not T feature)
        {
            feature = factory(this);
            _features.Add(feature);
        }

        configure?.Invoke(feature);
        return feature;
    }

    public IModule ConfigureHostedService<T>(int priority = 0)
    {
        _hostedServiceDescriptors.Add(new HostedServiceDescriptor(priority, typeof(T)));
        return this;
    }

    public void Apply()
    {
        var featureTypes = _features.Select(x => x.GetType()).TSort(x => x.GetCustomAttributes<DependsOn>().Select(dependsOn => dependsOn.Type)).ToList();
        var features = featureTypes.Select(featureType => _features.FirstOrDefault(x => x.GetType() == featureType) ?? (IFeature)Activator.CreateInstance(featureType, this)!).ToList();

        foreach (var feature in features)
        {
            feature.Configure();
            feature.ConfigureHostedServices();
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order))
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));

        foreach (var feature in features)
            feature.Apply();
    }
}