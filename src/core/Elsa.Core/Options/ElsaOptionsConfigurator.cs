using Elsa.ActivityNodeResolvers;
using Elsa.Implementations;
using Elsa.Pipelines.ActivityExecution;
using Elsa.Pipelines.WorkflowExecution;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Options;

public class ElsaOptionsConfigurator
{
    private readonly ISet<IConfigurator> _configurators = new HashSet<IConfigurator>();

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

    internal void ConfigureServices()
    {
        AddElsaCore();
        AddDefaultExpressionHandlers();

        foreach (var configurator in _configurators)
            configurator.ConfigureServices(Services);
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
            .AddSingleton<IActivityNodeResolver, OutboundActivityNodeResolver>()
            .AddSingleton<IHasher, Hasher>()
            .AddSingleton<IIdentityGenerator, RandomIdentityGenerator>()
            .AddSingleton<ISystemClock, SystemClock>()
            .AddSingleton<IBookmarkDataSerializer, BookmarkDataSerializer>()
            .AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>()

            // Expressions.
            .AddSingleton<IExpressionEvaluator, ExpressionEvaluator>()
            .AddSingleton<IExpressionHandlerRegistry, ExpressionHandlerRegistry>()

            // Pipelines.
            .AddSingleton<IActivityExecutionPipeline, ActivityExecutionPipeline>()
            .AddSingleton<IWorkflowExecutionPipeline, WorkflowExecutionPipeline>()

            // Logging
            .AddLogging();
    }

    private void AddDefaultExpressionHandlers()
    {
        Services.AddDefaultExpressionHandlers();
    }
}