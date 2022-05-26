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

    public T Configure<T>(Action<T>? configure = default) where T : class, IConfigurator, new() => Configure(() => new T(), configure);

    public T Configure<T>(Func<T> factory, Action<T>? configure = default) where T : class, IConfigurator
    {
        if (_configurators.FirstOrDefault(x => x is T) is not T configurator)
        {
            configurator = factory();
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

    public void RunConfigurators()
    {
        foreach (var configurator in _configurators)
        {
            configurator.ConfigureServices(this);
            configurator.ConfigureHostedServices(this);
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order)) 
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));
    }
}