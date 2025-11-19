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
