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
/// Helper class for unit testing activities in isolation.
/// Provides methods to execute activities with minimal setup, abstracting away the complexity 
/// of creating WorkflowExecutionContext and ActivityExecutionContext.
/// </summary>
public static class ActivityTestHelper
{
    /// <summary>
    /// Executes an activity in isolation for unit testing purposes and returns the execution context.
    /// This method handles all the complexity of setting up the execution context,
    /// evaluating inputs, and executing the activity.
    /// </summary>
    /// <param name="activity">The activity to execute</param>
    /// <param name="configureServices"></param>
    /// <returns>The ActivityExecutionContext used for execution</returns>
    public static async Task<ActivityExecutionContext> ExecuteActivityAsync(IActivity activity, Action<IServiceCollection>? configureServices = null)
    {
        var context = await CreateMinimalActivityExecutionContext(activity, configureServices);

        // Set up variables and inputs, then execute the activity
        await SetupExistingVariablesAsync(activity, context);
        await context.EvaluateInputPropertiesAsync();
        await activity.ExecuteAsync(context);

        return context;
    }

    /// <summary>
    /// Creates a minimal ActivityExecutionContext suitable for isolated unit testing of activities.
    /// This helper method creates a real WorkflowExecutionContext using the minimal workflow pattern
    /// to provide proper context for activities.
    /// </summary>
    private static async Task<ActivityExecutionContext> CreateMinimalActivityExecutionContext(IActivity activity, Action<IServiceCollection>? configureServices)
    {
        // Create a minimal service provider with the required services for expression evaluation
        var services = new ServiceCollection();

        // Add core services
        services.AddLogging();
        services.AddSingleton<ISystemClock>(_ => Substitute.For<ISystemClock>());
        services.AddSingleton<INotificationSender>(_ => Substitute.For<INotificationSender>());
        services.AddSingleton<IActivityVisitor, ActivityVisitor>();
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        services.AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
        services.AddSingleton<IActivityDescriber, ActivityDescriber>();
        services.AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>();
        services.AddSingleton<IActivityFactory, ActivityFactory>();
        services.AddSingleton<IPropertyDefaultValueResolver, PropertyDefaultValueResolver>();
        services.AddSingleton<IPropertyUIHandlerResolver, PropertyUIHandlerResolver>();
        services.AddSingleton<IActivityRegistry, ActivityRegistry>();
        services.AddScoped<IActivityRegistryLookupService, ActivityRegistryLookupService>();
        services.AddScoped<IIdentityGraphService, IdentityGraphService>();
        services.AddScoped<IWorkflowGraphBuilder, WorkflowGraphBuilder>();
        services.AddScoped<IActivityResolver, PropertyBasedActivityResolver>();
        services.AddScoped<IActivityResolver, SwitchActivityResolver>();
        services.AddScoped<DefaultActivityInputEvaluator>();

        // Add the default expression descriptor provider which includes Literal expressions
        services.AddSingleton<IExpressionDescriptorProvider, DefaultExpressionDescriptorProvider>();
        services.AddSingleton<IExpressionDescriptorRegistry, ExpressionDescriptorRegistry>();

        services.AddSingleton<IIdentityGenerator>(_ => Substitute.For<IIdentityGenerator>());

        services.AddSingleton<IHasher>(_ => Substitute.For<IHasher>());
        services.AddSingleton<ICommitStateHandler>(_ => Substitute.For<ICommitStateHandler>());
        services.AddSingleton<IActivitySchedulerFactory>(_ => Substitute.For<IActivitySchedulerFactory>());
        
        // Call the configure services action if provided.
        configureServices?.Invoke(services);

        var serviceProvider = services.BuildServiceProvider();
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        var workflowGraphBuilder = serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();
        await activityRegistry.RegisterAsync(activity.GetType());
        var workflow = Workflow.FromActivity(activity);
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);

        // Create workflow execution context using the static factory method
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            $"test-instance-{Guid.NewGuid()}",
            CancellationToken.None
        );

        // Create ActivityExecutionContext for the actual activity we want to test
        return await workflowExecutionContext.CreateActivityExecutionContextAsync(activity);
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
}