using Elsa.ActivityNodeResolvers;
using Elsa.Implementations;
using Elsa.Pipelines.ActivityExecution;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Elsa.Options;

public class ElsaOptionsConfigurator
{
    internal record HostedServiceDescriptor(int Order, Type HostedServiceType);

    private readonly ISet<IConfigurator> _configurators = new HashSet<IConfigurator>();
    private readonly ICollection<HostedServiceDescriptor> _hostedServiceDescriptors = new List<HostedServiceDescriptor>();

    public ElsaOptionsConfigurator(IServiceCollection services)
    {
        Services = services;
    }

    public IServiceCollection Services { get; }

    public T Configure<T>(Action<T>? configure = default) where T : class, IConfigurator, new() => Configure<T>(() => new T(), configure);

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

    public ElsaOptionsConfigurator AddHostedService<T>(int priority = 0)
    {
        _hostedServiceDescriptors.Add(new HostedServiceDescriptor(priority, typeof(T)));
        return this;
    }

    internal void ConfigureServices()
    {
        AddElsaCore();
        AddDefaultExpressionHandlers();

        foreach (var configurator in _configurators)
        {
            configurator.ConfigureServices(this);
            configurator.ConfigureHostedServices(this);
        }

        foreach (var hostedServiceDescriptor in _hostedServiceDescriptors.OrderBy(x => x.Order)) 
            Services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), hostedServiceDescriptor.HostedServiceType));
    }

    private IServiceCollection AddElsaCore()
    {
        Services.AddOptions<ElsaOptions>();

        return Services

            // Core.
            .AddSingleton<IActivityInvoker, ActivityInvoker>()
            .AddSingleton<IWorkflowRunner, WorkflowRunner>()
            .AddSingleton<IActivityWalker, ActivityWalker>()
            .AddSingleton<IIdentityGraphService, IdentityGraphService>()
            .AddSingleton<IWorkflowStateSerializer, WorkflowStateSerializer>()
            .AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IIdentityGenerator, RandomIdentityGenerator>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<IBookmarkDataSerializer, BookmarkDataSerializer>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline, ActivityExecutionPipeline>()
            .AddSingleton<IWorkflowExecutionPipeline, WorkflowExecutionPipeline>()
            
            // Built-in activity services.
            .AddSingleton<IActivityNodeResolver, OutboundActivityNodeResolver>()
            .AddSingleton<IActivityNodeResolver, SwitchActivityNodeResolver>()
            .AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>()

            // Logging
            .AddLogging();
    }

    private void AddDefaultExpressionHandlers()
    {
        Services.AddDefaultExpressionHandlers();
    }
}