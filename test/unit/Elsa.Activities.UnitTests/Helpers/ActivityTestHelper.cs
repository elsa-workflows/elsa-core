using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Services;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Management.Providers;
using Elsa.Workflows.Management.Services;
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
    /// <returns>Task representing the async execution</returns>
    public static async Task<ActivityExecutionContext> ExecuteActivityAsync(IActivity activity)
    {
        var context = CreateMinimalActivityExecutionContext(activity, out var serviceProvider);
        
        // Set up variables and inputs, then execute the activity
        await SetupExistingVariablesAsync(activity, context);
        await SetupInputValuesInMemoryAsync(activity, context, serviceProvider);
        await context.EvaluateInputPropertiesAsync();
        await activity.ExecuteAsync(context);
        return context;
    }

    /// <summary>
    /// Creates a minimal ActivityExecutionContext suitable for isolated unit testing of activities.
    /// This helper method creates a real WorkflowExecutionContext using the minimal workflow pattern
    /// to provide proper context for activities.
    /// </summary>
    private static ActivityExecutionContext CreateMinimalActivityExecutionContext(IActivity activity, out IServiceProvider serviceProvider)
    {
        // Create a minimal service provider with the required services for expression evaluation
        var services = new ServiceCollection();
        
        // Add core services
        services.AddSingleton<ISystemClock>(_ => Substitute.For<ISystemClock>());
        services.AddSingleton<INotificationSender>(_ => Substitute.For<INotificationSender>());
        services.AddSingleton<IActivityVisitor, ActivityVisitor>();
        
        // Add real expression evaluation services instead of mocks
        services.AddScoped<IExpressionEvaluator, ExpressionEvaluator>();
        
        // Add the well-known type registry required by expression handlers
        services.AddSingleton<IWellKnownTypeRegistry, WellKnownTypeRegistry>();
        
        // Add the default expression descriptor provider which includes Literal expressions
        services.AddSingleton<IExpressionDescriptorProvider, DefaultExpressionDescriptorProvider>();
        services.AddSingleton<IExpressionDescriptorRegistry, ExpressionDescriptorRegistry>();
        
        services.AddSingleton<IIdentityGenerator>(_ => Substitute.For<IIdentityGenerator>());
        
        // Mock the complex workflow-level dependencies  
        services.AddSingleton<IActivityRegistry>(_ => Substitute.For<IActivityRegistry>());
        
        // Set up the activity registry lookup service to return proper descriptors
        var activityRegistryLookup = Substitute.For<IActivityRegistryLookupService>();
        activityRegistryLookup.FindAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(callInfo =>
        {
            var activityType = callInfo.ArgAt<string>(0);
            return Task.FromResult<ActivityDescriptor?>(new ActivityDescriptor
            {
                TypeName = activityType,
                Kind = ActivityKind.Action,
                Category = "Test",
                Description = "Test activity for unit testing",
                Version = 1
            });
        });
        
        services.AddSingleton<IActivityRegistryLookupService>(_ => activityRegistryLookup);
        
        services.AddSingleton<IIdentityGraphService>(_ => Substitute.For<IIdentityGraphService>());
        services.AddSingleton<IWorkflowGraphBuilder>(_ => Substitute.For<IWorkflowGraphBuilder>());
        services.AddSingleton<IHasher>(_ => Substitute.For<IHasher>());
        services.AddSingleton<ICommitStateHandler>(_ => Substitute.For<ICommitStateHandler>());
        services.AddSingleton<IActivitySchedulerFactory>(_ => Substitute.For<IActivitySchedulerFactory>());
        
        serviceProvider = services.BuildServiceProvider();
        
        // Create a minimal workflow
        activity.Id ??= $"test-activity-{Guid.NewGuid()}";
        
        var workflow = new Workflow
        {
            Root = activity
        };
        
        // Create a simple workflow graph manually instead of using the builder
        var rootNode = new ActivityNode(activity, "Root");
        var nodes = new List<ActivityNode> { rootNode };
        var workflowGraph = new WorkflowGraph(workflow, rootNode, nodes);
        
        // Create workflow execution context using the static factory method
        var workflowExecutionContext = WorkflowExecutionContext.CreateAsync(
            serviceProvider, 
            workflowGraph, 
            $"test-instance-{Guid.NewGuid()}", 
            CancellationToken.None
        ).GetAwaiter().GetResult();
        
        // Create ActivityExecutionContext for the actual activity we want to test
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContextAsync(activity)
            .GetAwaiter().GetResult();
        
        return activityExecutionContext;
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
                        p.PropertyType.BaseType == typeof(Variable))
            .ToList();
        
        foreach (var variable in variableProperties.Select(property => (Variable)property.GetValue(activity)!))
        {
            variable.Set(context.ExpressionExecutionContext, variable.Value);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Sets up input values in memory blocks so that context.Get() can find them during activity execution.
    /// This mimics what the workflow engine does when evaluating inputs.
    /// </summary>
    private static async Task SetupInputValuesInMemoryAsync(IActivity activity, ActivityExecutionContext context, IServiceProvider serviceProvider)
    {
        var activityType = activity.GetType();
        var inputProperties = activityType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(Input<>))
            .ToList();

        foreach (var input in inputProperties.Select(property => property.GetValue(activity) as Input))
        {
            if (input?.Expression == null)
            {
                continue;
            }

            // Get the memory block reference for this input
            var memoryBlockReference = input.MemoryBlockReference();
                
            // Evaluate the input using the expression evaluator
            var expressionEvaluator = serviceProvider.GetService<IExpressionEvaluator>();
            var evaluatedValue = await expressionEvaluator!.EvaluateAsync(input, context.ExpressionExecutionContext);
                
            // Set the value in the memory block
            memoryBlockReference.Set(context, evaluatedValue);
        }
    }
}
