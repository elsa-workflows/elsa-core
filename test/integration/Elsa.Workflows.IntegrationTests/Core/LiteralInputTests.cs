using Elsa.Expressions.Models;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.Core;

/// <summary>
/// Tests for ActivityExecutionContext handling of Literal inputs in real scenarios
/// </summary>
public class LiteralInputTests
{
    private readonly IServiceProvider _services;
    private readonly IWorkflowRunner _workflowRunner;

    public LiteralInputTests(ITestOutputHelper testOutputHelper)
    {
        _services = new TestApplicationBuilder(testOutputHelper).Build();
        _workflowRunner = _services.GetRequiredService<IWorkflowRunner>();
    }

    [Fact(DisplayName = "Activity should be able to access Input created with Literal")]
    public async Task ActivityShouldAccessLiteralInput()
    {
        // Arrange - Create a custom activity that uses another activity with literal input
        await _services.PopulateRegistriesAsync();
        
        var workflow = new TestWorkflow(builder =>
        {
            builder.Root = new CompositeActivityWithLiteralInput();
        });

        // Act & Assert - Should not throw
        var result = await _workflowRunner.RunAsync(workflow);
        Assert.Equal(WorkflowSubStatus.Finished, result.WorkflowState.SubStatus);
    }

    [Fact(DisplayName = "ActivityExecutionContext.TryGet should return true and value for Literal")]
    public async Task TryGet_ShouldHandleLiteralDirectly()
    {
        // Arrange - Create a workflow and activity execution context
        await _services.PopulateRegistriesAsync();
        
        var activity = new WriteLine("Test");
        var workflow = new Workflow { Root = activity };
        
        var workflowGraphBuilder = _services.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_services, workflowGraph, "test");
        var activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity);
        
        // Create a Literal and use it as a MemoryBlockReference
        var expectedValue = "Hello World";
        var literal = new Literal<string>(expectedValue);
        var blockReference = (MemoryBlockReference)literal;
        
        // Act - Call TryGet directly with the Literal
        var success = activityExecutionContext.TryGet(blockReference, out var actualValue);
        
        // Assert - Should succeed and return the literal's value
        Assert.True(success, "TryGet should return true for Literal references");
        Assert.Equal(expectedValue, actualValue);
    }

    [Fact(DisplayName = "ActivityExecutionContext.Get with Input<T> containing Literal should work")]
    public async Task Get_ShouldWorkWithInputContainingLiteral()
    {
        // Arrange
        await _services.PopulateRegistriesAsync();
        
        var activity = new WriteLine("Test");
        var workflow = new Workflow { Root = activity };
        
        var workflowGraphBuilder = _services.GetRequiredService<IWorkflowGraphBuilder>();
        var workflowGraph = await workflowGraphBuilder.BuildAsync(workflow);
        var workflowExecutionContext = await WorkflowExecutionContext.CreateAsync(_services, workflowGraph, "test");
        var activityExecutionContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(activity);
        
        // Create an Input with a Literal value
        var expectedValue = 42;
        var literal = new Literal<int>(expectedValue);
        var input = new Input<int>(literal);
        
        // Act - Get the value through the Input (which internally uses TryGet)
        var actualValue = activityExecutionContext.Get(input);
        
        // Assert
        Assert.Equal(expectedValue, actualValue);
    }
}

/// <summary>
/// A composite activity that creates inputs with literal values and tries to read them
/// This simulates the use case described in the issue where activities re-use other activities' execute methods
/// </summary>
public class CompositeActivityWithLiteralInput : CodeActivity
{
    protected override ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // Create an Input with a Literal value - this is a common pattern when programmatically
        // creating activities and setting their inputs
        var literal = new Literal<string>("Test Value");
        var input = new Input<string>(literal);
        
        // Try to get the value - this should work but will fail without the Literal handling in TryGet
        // The issue is that when Input is created with a Literal, the Literal becomes the MemoryBlockReference
        // When Get is called, it tries to find this in the memory register, but Literals hold values directly
        var value = context.Get(input);
        
        // If we got here without exception, the test passes
        if (value != "Test Value")
            throw new Exception($"Expected 'Test Value' but got '{value}'");
            
        return ValueTask.CompletedTask;
    }
}
