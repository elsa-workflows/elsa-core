using Elsa.Common;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Primitives;

public class SetVariableTests
{
    [Fact]
    public async Task Should_Set_Variable()
    {
        // Arrange
        var variable = new Variable<int>("myVar", 0);
        var setVariable = new SetVariable<int>(variable, new Input<int>(42));
        var context = CreateMinimalActivityExecutionContext(setVariable);
        
        // Prepare the activity inputs in the context
        //PrepareActivityInputs(setVariable, context);
        
        // Act
        await ExecuteActivityAsync(setVariable, context);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal(42, result);
    }

    [Fact]
    public async Task Should_Set_Variable_From_Expression()
    {
        // Arrange
        var variable = new Variable<string>("myStringVar", "");
        var setVariable = new SetVariable<string>(variable, new Input<string>("Hello World"));
        var context = CreateMinimalActivityExecutionContext(setVariable);
        
        // Prepare the activity inputs in the context
        PrepareActivityInputs(setVariable, context);
        
        // Act
        await ExecuteActivityAsync(setVariable, context);
        
        // Assert
        var result = variable.Get(context);
        Assert.Equal("Hello World", result);
    }

    /// <summary>
    /// Creates a minimal ActivityExecutionContext suitable for isolated unit testing of activities.
    /// This helper method creates a real WorkflowExecutionContext using the minimal workflow pattern
    /// to provide proper context for activities like SetVariable.
    /// </summary>
    private static ActivityExecutionContext CreateMinimalActivityExecutionContext(IActivity activity)
    {
        // Create a minimal service provider with just the required services
        var services = new ServiceCollection();
        services.AddSingleton<ISystemClock>(_ => Substitute.For<ISystemClock>());
        services.AddSingleton<INotificationSender>(_ => Substitute.For<INotificationSender>());
        services.AddSingleton<IActivityVisitor, ActivityVisitor>();
        
        // Mock the complex dependencies to avoid deep dependency chains
        services.AddSingleton<IActivityRegistry>(_ => Substitute.For<IActivityRegistry>());
        
        // Set up the activity registry lookup service to return proper descriptors for SetVariable activities
        var activityRegistryLookup = Substitute.For<IActivityRegistryLookupService>();
        activityRegistryLookup.FindAsync(Arg.Any<string>(), Arg.Any<int>()).Returns(callInfo =>
        {
            var activityType = callInfo.ArgAt<string>(0);
            return Task.FromResult<ActivityDescriptor?>(new ActivityDescriptor
            {
                TypeName = activityType,
                Kind = ActivityKind.Action,
                Category = "Primitives",
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
        services.AddSingleton<IIdentityGenerator>(_ => Substitute.For<IIdentityGenerator>());
        
        var serviceProvider = services.BuildServiceProvider();
        
        // Create a minimal workflow
        activity.Id = "test-workflow-activity"; // Ensure the activity has an ID
        
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
            "test-instance", 
            CancellationToken.None
        ).GetAwaiter().GetResult();
        
        // Create ActivityExecutionContext for the actual activity we want to test
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContextAsync(activity)
            .GetAwaiter().GetResult();
        
        return activityExecutionContext;
    }

    /// <summary>
    /// Helper method to execute an activity using reflection to access the protected ExecuteAsync method.
    /// This enables testing activities in isolation without requiring the full workflow engine.
    /// </summary>
    private static async Task ExecuteActivityAsync(IActivity activity, ActivityExecutionContext context)
    {
        try
        {
            await context.EvaluateInputPropertiesAsync();
            await activity.ExecuteAsync(context);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    /// <summary>
    /// Prepares activity inputs by declaring their memory blocks in the execution context.
    /// This simulates what the workflow engine does to make input values available to activities.
    /// Call this method before executing an activity to ensure its inputs are properly set up.
    /// </summary>
    private static void PrepareActivityInputs(IActivity activity, ActivityExecutionContext context)
    {
        Console.WriteLine($"Preparing inputs for activity: {activity.GetType().Name}");
        
        var properties = activity.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            // Look for Input<T> properties
            if (property.PropertyType.IsGenericType && 
                property.PropertyType.GetGenericTypeDefinition() == typeof(Input<>))
            {
                Console.WriteLine($"Found Input property: {property.Name}");
                
                var inputValue = property.GetValue(activity);
                if (inputValue != null)
                {
                    // Get the MemoryBlockReference from the Input<T>
                    var memoryBlockRefProperty = inputValue.GetType().GetProperty("MemoryBlockReference");
                    if (memoryBlockRefProperty?.GetValue(inputValue) is MemoryBlockReference memoryBlockRef)
                    {
                        Console.WriteLine($"MemoryBlockReference ID: {memoryBlockRef.Id}");
                        
                        // Get the expression from the Input<T>
                        var expressionProperty = inputValue.GetType().GetProperty("Expression");
                        var expression = expressionProperty?.GetValue(inputValue);
                        
                        if (expression != null)
                        {
                            Console.WriteLine($"Expression type: {expression.GetType().Name}");
                            
                            // For Literal<T> expressions, manually create and register the memory block
                            if (expression.GetType().IsGenericType && 
                                expression.GetType().GetGenericTypeDefinition() == typeof(Literal<>))
                            {
                                var valueProperty = expression.GetType().GetProperty("Value");
                                var literalValue = valueProperty?.GetValue(expression);
                                
                                Console.WriteLine($"Literal value: {literalValue}");
                                
                                // Create a memory block with the literal value directly
                                var memoryBlock = new MemoryBlock(literalValue);
                                
                                // Register it in the memory system using the memory block reference ID
                                context.ExpressionExecutionContext.Memory.Blocks[memoryBlockRef.Id] = memoryBlock;
                                
                                Console.WriteLine($"Registered memory block with ID: {memoryBlockRef.Id}");
                                Console.WriteLine($"Total blocks in memory: {context.ExpressionExecutionContext.Memory.Blocks.Count}");
                                
                                // Also try to register in workflow execution context memory if different
                                if (context.WorkflowExecutionContext.ExpressionExecutionContext.Memory != context.ExpressionExecutionContext.Memory)
                                {
                                    context.WorkflowExecutionContext.ExpressionExecutionContext.Memory.Blocks[memoryBlockRef.Id] = memoryBlock;
                                    Console.WriteLine("Also registered in workflow execution context memory");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        // List all memory blocks for debugging
        Console.WriteLine("All memory blocks:");
        foreach (var block in context.ExpressionExecutionContext.Memory.Blocks)
        {
            Console.WriteLine($"  {block.Key} -> {block.Value.Value}");
        }
    }
}