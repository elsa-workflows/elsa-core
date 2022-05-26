using System.Reflection;
using Elsa.ServiceConfiguration.Attributes;
using Elsa.ServiceConfiguration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.ServiceConfiguration.Implementations;

public class ServiceConfiguration : IServiceConfiguration
{
    private record HostedServiceDescriptor(int Order, Type HostedServiceType);

    private readonly ISet<IConfigurator> _configurators = new HashSet<IConfigurator>();
    private readonly ICollection<HostedServiceDescriptor> _hostedServiceDescriptors = new List<HostedServiceDescriptor>();

    public ServiceConfiguration(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public T Configure<T>(Action<T>? configure = default) where T : class, IConfigurator => Configure(serviceConfiguration => (T)Activator.CreateInstance(typeof(T), serviceConfiguration)!, configure);

    public T Configure<T>(Func<IServiceConfiguration, T> factory, Action<T>? configure = default) where T : class, IConfigurator
    {
        if (_configurators.FirstOrDefault(x => x is T) is not T configurator)
        {
            configurator = factory(this);
            _configurators.Add(configurator);
        }

        configure?.Invoke(configurator);
        return configurator;
    }

    public IServiceConfiguration ConfigureHostedService<T>(int priority = 0)
    {
        _hostedServiceDescriptors.Add(new HostedServiceDescriptor(priority, typeof(T)));
        return this;
    }

    public void Apply()
    {
        ResolveDependencies();

        foreach (var configurator in _configurators)
        {
            configurator.ConfigureServices(this);
            configurator.ConfigureHostedServices(this);
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order))
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));
    }

    private void ResolveDependencies()
    {
        var resolvedDependencyTypes = new HashSet<Type>();
        
        foreach (var configurator in _configurators.ToList())
            ResolveDependencies(configurator, resolvedDependencyTypes);
    }

    private void ResolveDependencies(IConfigurator configurator, ISet<Type> resolvedDependencyTypes)
    {
        var dependencyTypes = configurator.GetType().GetCustomAttributes<DependencyAttribute>().Select(x => x.Type).ToHashSet();
        dependencyTypes = dependencyTypes.Except(resolvedDependencyTypes).ToHashSet(); 

        foreach (var type in dependencyTypes)
        {
            var dependencyConfigurator = AddConfigurator(type);
            resolvedDependencyTypes.Add(type);
            ResolveDependencies(dependencyConfigurator, resolvedDependencyTypes);
        }
    }

    private IConfigurator AddConfigurator(Type type)
    {
        var configurator = _configurators.FirstOrDefault(x => x.GetType() == type);

        if (configurator != null)
            return configurator;

        configurator = (IConfigurator)Activator.CreateInstance(type, this)!;
        _configurators.Add(configurator);
        return configurator;
    }
}