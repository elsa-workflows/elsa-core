using System.Net;
using System.Text;
using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Elsa.Http.Parsers;
using Elsa.Mediator.Contracts;
using Elsa.Resilience;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
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
    /// Executes an activity for unit testing purposes with comprehensive configuration options.
    /// This unified method handles all activity execution scenarios. When child activities are provided,
    /// background execution mode is automatically enabled to capture scheduled activities.
    /// </summary>
    /// <param name="activity">The activity to execute</param>
    /// <param name="configureServices">Optional action to configure the service collection for dependency injection</param>
    /// <param name="childActivities">Optional child activities that should be part of the workflow graph. When provided, automatically enables background execution mode to capture scheduled activities.</param>
    /// <returns>The ActivityExecutionContext used for execution</returns>
    public static async Task<ActivityExecutionContext> ExecuteActivityAsync(
        IActivity activity, 
        Action<IServiceCollection>? configureServices = null)
    {
        var context = await CreateMinimalActivityExecutionContext(activity, configureServices);
        
        // Set up variables and inputs, then execute the activity
        await SetupExistingVariablesAsync(activity, context);
        await context.EvaluateInputPropertiesAsync();
        await activity.ExecuteAsync(context);

        return context;
    }

    /// <summary>
    /// Creates a simple HTTP response message with specified status code and optional JSON content.
    /// </summary>
    /// <param name="statusCode">The HTTP status code for the response</param>
    /// <param name="jsonContent">Optional JSON content for the response body</param>
    /// <param name="additionalHeaders">Optional additional headers to add to the response</param>
    /// <returns>An HttpResponseMessage configured with the specified parameters</returns>
    public static HttpResponseMessage CreateHttpResponse(HttpStatusCode statusCode, string? jsonContent = null, Dictionary<string, string>? additionalHeaders = null)
    {
        var response = new HttpResponseMessage(statusCode);
        
        if (jsonContent != null)
        {
            response.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        }
        
        if (additionalHeaders != null)
        {
            foreach (var header in additionalHeaders)
            {
                response.Headers.Add(header.Key, header.Value);
            }
        }
        
        return response;
    }

    /// <summary>
    /// Configures services commonly needed for HTTP activity testing.
    /// This is a convenience method that combines HTTP services, mock HTTP client factory, and mock resilient invoker.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="responseHandler">Handler for HTTP requests, or null to use a default OK response</param>
    public static void ConfigureHttpActivityServices(IServiceCollection services, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? responseHandler = null)
    {
        var defaultHandler = responseHandler ?? ((_, _) => Task.FromResult(CreateHttpResponse(HttpStatusCode.OK)));
        var mockHttpClientFactory = CreateMockHttpClientFactory(defaultHandler);
        
        services.AddSingleton(mockHttpClientFactory);
        services.AddSingleton(CreateMockResilientActivityInvoker());
        AddHttpServices(services);
        services.AddLogging();
    }

    /// <summary>
    /// Validates that an activity has the expected attribute configuration.
    /// This is useful for ensuring activities are properly decorated with metadata.
    /// </summary>
    /// <param name="activityType">The type of activity to validate</param>
    /// <param name="expectedNamespace">Expected namespace (e.g., "Elsa")</param>
    /// <param name="expectedCategory">Expected category (e.g., "HTTP")</param>
    /// <param name="expectedDisplayName">Expected display name</param>
    /// <param name="expectedDescription">Expected description</param>
    /// <param name="expectedKind">Expected activity kind</param>
    public static void AssertActivityAttributes(
        Type activityType, 
        string expectedNamespace,
        ActivityKind expectedKind, 
        string? expectedCategory = null, 
        string? expectedDisplayName = null, 
        string? expectedDescription = null)
    {
        var activityAttribute = activityType.GetCustomAttributes(typeof(ActivityAttribute), false)
            .Cast<ActivityAttribute>().FirstOrDefault();

        Assert.NotNull(activityAttribute);
        Assert.Equal(expectedNamespace, activityAttribute.Namespace);
        Assert.Equal(expectedKind, activityAttribute.Kind);
        
        if(expectedCategory != null) Assert.Equal(expectedCategory, activityAttribute.Category);
        if(expectedDescription != null) Assert.Equal(expectedDescription, activityAttribute.Description);
        if(expectedDisplayName != null) Assert.Equal(expectedDisplayName, activityAttribute.DisplayName);
    }

    /// <summary>
    /// Creates a mock activity for testing purposes.
    /// This is useful for creating placeholder activities when testing activity scheduling behavior.
    /// </summary>
    /// <returns>A mock IActivity instance</returns>
    public static IActivity CreateMockActivity() => Substitute.For<IActivity>();

    /// <summary>
    /// Adds all HTTP-related services to the service collection.
    /// This includes content factories, parsers, and HTTP client services.
    /// Use this method when testing HTTP activities that require full HTTP service support.
    /// </summary>
    /// <param name="services">The service collection to add HTTP services to</param>
    private static void AddHttpServices(IServiceCollection services)
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
    private static IResilientActivityInvoker CreateMockResilientActivityInvoker()
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
    /// Creates a mock HTTP client factory with a test handler for controlled HTTP responses.
    /// </summary>
    /// <param name="responseHandler">Function to handle requests and return responses</param>
    /// <returns>A mock IHttpClientFactory configured with the test handler</returns>
    private static IHttpClientFactory CreateMockHttpClientFactory(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        var mockHttpClientFactory = Substitute.For<IHttpClientFactory>();
        var testHandler = new TestHttpMessageHandler(responseHandler);
        var httpClient = new HttpClient(testHandler);
        mockHttpClientFactory.CreateClient(Arg.Any<string>()).Returns(httpClient);
        return mockHttpClientFactory;
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
        AddCoreWorkflowServices(services);
        
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


    /// <summary>
    /// Creates a workflow that includes all the provided activities to ensure they are part of the workflow graph.
    /// This is necessary for testing activities that schedule child activities.
    /// </summary>
    /// <param name="rootActivity">The main activity to execute</param>
    /// <param name="childActivities">Additional activities that should be part of the workflow graph</param>
    /// <param name="configureServices">Optional service configuration</param>
    /// <returns>The ActivityExecutionContext for the root activity</returns>
    private static async Task<ActivityExecutionContext> CreateActivityExecutionContextWithChildActivities(
        IActivity rootActivity, 
        IEnumerable<IActivity> childActivities,
        Action<IServiceCollection>? configureServices = null)
    {
        // Create a workflow that includes all activities
        var allActivities = new[] { rootActivity }.Concat(childActivities).ToArray();
        
        // Create a service provider.
        var services = new ServiceCollection();
        AddCoreWorkflowServices(services);
        configureServices?.Invoke(services);
        var serviceProvider = services.BuildServiceProvider();
        
        // Register all activities
        var activityRegistry = serviceProvider.GetRequiredService<IActivityRegistry>();
        foreach (var activity in allActivities)
        {
            await activityRegistry.RegisterAsync(activity.GetType());
        }
        
        // Create a sequence workflow that contains all activities
        var workflow = new Workflow
        {
            Root = new Sequence
            {
                Activities = allActivities.ToList()
            }
        };
        
        var workflowGraphBuilder = serviceProvider.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);

        // Create workflow execution context
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            serviceProvider,
            workflowGraph,
            $"test-instance-{Guid.NewGuid()}",
            CancellationToken.None
        );

        // Create ActivityExecutionContext for the root activity
        return await workflowExecutionContext.CreateActivityExecutionContextAsync(rootActivity);
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
