using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Http.ContentWriters;
using Elsa.Http.Parsers;
using Elsa.Mediator.Contracts;
using Elsa.Resilience;
using Elsa.Workflows;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.PortResolvers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Elsa.Http;

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
    /// <param name="configureServices">An optional action to configure the service collection for dependency injection during activity execution.</param>
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
    /// Adds all HTTP-related services to the service collection.
    /// This includes content factories, parsers, and HTTP client services.
    /// Use this method when testing HTTP activities that require full HTTP service support.
    /// </summary>
    /// <param name="services">The service collection to add HTTP services to</param>
    public static void AddHttpServices(IServiceCollection services)
    {
        // Add all required HTTP services
        services.AddSingleton<IHttpContentFactory, JsonContentFactory>();
        services.AddSingleton<IHttpContentFactory, TextContentFactory>();
        services.AddSingleton<IHttpContentFactory, XmlContentFactory>();
        services.AddSingleton<IHttpContentFactory, FormUrlEncodedHttpContentFactory>();
        
        // Add HTTP content parsers
        AddHttpContentParsers(services);
        
        // Add other required services
        services.AddHttpClient();
    }
    
    /// <summary>
    /// Creates a mock IResilientActivityInvoker that directly executes the provided action.
    /// Useful for testing activities that depend on resilient execution without the complexity of retry policies.
    /// </summary>
    /// <returns>A mock IResilientActivityInvoker configured to execute actions directly</returns>
    public static IResilientActivityInvoker CreateMockResilientActivityInvoker()
    {
        var mock = Substitute.For<IResilientActivityInvoker>();
        
        // Configure the mock to simply execute the provided action directly
        mock.InvokeAsync(
                Arg.Any<IResilientActivity>(), 
                Arg.Any<ActivityExecutionContext>(), 
                Arg.Any<Func<Task<HttpResponseMessage>>>(), 
                Arg.Any<CancellationToken>())
            .Returns(callInfo => 
            {
                var action = callInfo.ArgAt<Func<Task<HttpResponseMessage>>>(2);
                return action.Invoke();
            });
            
        return mock;
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
    
    /// <summary>
    /// Adds HTTP content parsers to the service collection.
    /// These parsers are responsible for parsing different content types in HTTP responses.
    /// </summary>
    /// <param name="services">The service collection to add parsers to</param>
    private static void AddHttpContentParsers(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentParser, JsonHttpContentParser>();
        services.AddSingleton<IHttpContentParser, PlainTextHttpContentParser>();
        services.AddSingleton<IHttpContentParser, XmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, TextHtmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, FileHttpContentParser>();
    }
}
