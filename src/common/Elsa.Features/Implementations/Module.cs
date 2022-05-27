using System.Reflection;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Features.Implementations;

public class Module : IModule
{
    private record HostedServiceDescriptor(int Order, Type HostedServiceType);

    private readonly ISet<IFeature> _configurators = new HashSet<IFeature>();
    private readonly ICollection<HostedServiceDescriptor> _hostedServiceDescriptors = new List<HostedServiceDescriptor>();

    public Module(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public T Configure<T>(Action<T>? configure = default) where T : class, IFeature => Configure(serviceConfiguration => (T)Activator.CreateInstance(typeof(T), serviceConfiguration)!, configure);

    public T Configure<T>(Func<IModule, T> factory, Action<T>? configure = default) where T : class, IFeature
    {
        if (_configurators.FirstOrDefault(x => x is T) is not T configurator)
        {
            configurator = factory(this);
            _configurators.Add(configurator);
        }

        configure?.Invoke(configurator);
        return configurator;
    }

    public IModule ConfigureHostedService<T>(int priority = 0)
    {
        _hostedServiceDescriptors.Add(new HostedServiceDescriptor(priority, typeof(T)));
        return this;
    }

    public void Apply()
    {
        ResolveDependencies();

        foreach (var configurator in _configurators)
        {
            configurator.Configure();
            configurator.ConfigureHostedServices();
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order))
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));
        
        foreach (var configurator in _configurators)
        {
            configurator.Apply();
        }
    }

    private void ResolveDependencies()
    {
        var resolvedDependencyTypes = new HashSet<Type>();
        
        foreach (var configurator in _configurators.ToList())
            ResolveDependencies(configurator, resolvedDependencyTypes);
    }

    private void ResolveDependencies(IFeature feature, ISet<Type> resolvedDependencyTypes)
    {
        var dependencyTypes = feature.GetType().GetCustomAttributes<DependsOn>().Select(x => x.Type).ToHashSet();
        dependencyTypes = dependencyTypes.Except(resolvedDependencyTypes).ToHashSet(); 

        foreach (var type in dependencyTypes)
        {
            var dependencyConfigurator = AddConfigurator(type);
            resolvedDependencyTypes.Add(type);
            ResolveDependencies(dependencyConfigurator, resolvedDependencyTypes);
        }
    }

    private IFeature AddConfigurator(Type type)
    {
        var configurator = _configurators.FirstOrDefault(x => x.GetType() == type);

        if (configurator != null)
            return configurator;

        configurator = (IFeature)Activator.CreateInstance(type, this)!;
        _configurators.Add(configurator);
        return configurator;
    }
}