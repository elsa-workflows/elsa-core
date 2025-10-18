using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.PortResolvers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Helpers;

/// <summary>
/// A test fixture for unit testing activities in isolation.
/// Provides a fluent API to configure services, variables, and execution context.
/// </summary>
public class ActivityTestFixture
{
    private readonly IActivity _activity;
    private readonly IServiceCollection _services;
    private readonly List<Action<ActivityExecutionContext>> _configureContextActions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityTestFixture"/> class.
    /// </summary>
    /// <param name="activity">The activity to test</param>
    public ActivityTestFixture(IActivity activity)
    {
        _activity = activity;
        _services = new ServiceCollection();
        AddCoreWorkflowServices(_services);
    }

    /// <summary>
    /// Gets the service collection for registering additional services.
    /// Use this to add services required by the activity under test.
    /// </summary>
    public IServiceCollection Services => _services;

    /// <summary>
    /// Configures the service collection using a fluent action.
    /// </summary>
    /// <param name="configure">Action to configure the service collection</param>
    /// <returns>The fixture instance for method chaining</returns>
    public ActivityTestFixture ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    /// Executes the activity and returns the execution context.
    /// </summary>
    /// <returns>The ActivityExecutionContext after execution</returns>
    public async Task<ActivityExecutionContext> ExecuteAsync()
    {
        var context = await BuildAsync();

        // Set up variables and inputs, then execute the activity
        await SetupExistingVariablesAsync(_activity, context);
        await context.EvaluateInputPropertiesAsync();
        await _activity.ExecuteAsync(context);

        return context;
    }

    /// <summary>
    /// Builds the ActivityExecutionContext without executing the activity.
    /// </summary>
    private async Task<ActivityExecutionContext> BuildAsync()
    {
        var serviceProvider = _services.BuildServiceProvider();
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        var workflowGraphBuilder = serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();

        await activityRegistry.RegisterAsync(_activity.GetType());

        var workflow = Workflow.FromActivity(_activity);
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);

        // Create workflow execution context using the static factory method
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            $"test-instance-{Guid.NewGuid()}",
            CancellationToken.None
        );

        // Create ActivityExecutionContext for the actual activity we want to test
        var context = await workflowExecutionContext.CreateActivityExecutionContextAsync(_activity);

        // Apply any context configuration actions
        foreach (var configureAction in _configureContextActions)
        {
            configureAction(context);
        }

        return context;
    }

    /// <summary>
    /// Sets up existing variables found on the activity in the execution context.
    /// This is necessary because in unit tests, variables need to be initialized.
    /// </summary>
    private static Task SetupExistingVariablesAsync(IActivity activity, ActivityExecutionContext context)
    {
        var activityType = activity.GetType();
        var variableProperties = activityType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(Variable<>))
            .ToList();

        foreach (var variable in variableProperties.Select(property => (Variable)property.GetValue(activity)!))
        {
            variable.Set(context.ExpressionExecutionContext, variable.Value);
        }

        return Task.CompletedTask;
    }

    private static void AddCoreWorkflowServices(IServiceCollection services)
    {
        services.AddLogging();
        services.AddSingleton<ISystemClock>(_ => Substitute.For<ISystemClock>());
        services.AddSingleton<INotificationSender>(_ => Substitute.For<INotificationSender>());
        services.AddSingleton<IActivityVisitor, ActivityVisitor>();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
        services.AddSingleton<IActivityDescriber, ActivityDescriber>();
        services.AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>();
        services.AddSingleton<IActivityFactory, ActivityFactory>();
        services.AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>();
        services.AddSingleton<IActivityRegistry, ActivityRegistry>();
        services.AddScoped<IActivityRegistryLookupService, ActivityRegistryLookupService>();
        services.AddScoped<IIdentityGraphService, IdentityGraphService>();
        services.AddScoped<IWorkflowGraphBuilder, WorkflowGraphBuilder>();
        services.AddScoped<IActivityResolver, PropertyBasedActivityResolver>();
        services.AddScoped<IActivityResolver, SwitchActivityResolver>();
        services.AddScoped<DefaultActivityInputEvaluator>();
        services.AddSingleton<IExpressionDescriptorProvider, DefaultExpressionDescriptorProvider>();
        services.AddSingleton<IExpressionDescriptorRegistry, ExpressionDescriptorRegistry>();
        services.AddSingleton<IIdentityGenerator>(_ => Substitute.For<IIdentityGenerator>());
        services.AddSingleton<IHasher>(_ => Substitute.For<IHasher>());
        services.AddSingleton<ICommitStateHandler>(_ => Substitute.For<ICommitStateHandler>());
        services.AddSingleton<IActivitySchedulerFactory, ActivitySchedulerFactory>();
        services.AddSingleton<IWorkflowExecutionContextSchedulerStrategy, FakeWorkflowExecutionContextSchedulerStrategy>();
        services.AddSingleton<IActivityExecutionContextSchedulerStrategy, FakeActivityExecutionContextSchedulerStrategy>();
    }
}
