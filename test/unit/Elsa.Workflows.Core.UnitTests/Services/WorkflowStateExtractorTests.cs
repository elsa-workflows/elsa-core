using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Options;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Services;

public class WorkflowStateExtractorTests
{
    [Fact]
    public async Task Extract_And_Apply_PreservesCallStackDepth()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;
        
        var contextA = await workflowExecutionContext.CreateActivityExecutionContextAsync(root);
        contextA.CallStackDepth = 10; // Manually set for testing persistence
        workflowExecutionContext.AddActivityExecutionContext(contextA);

        var extractor = workflowExecutionContext.GetRequiredService<IWorkflowStateExtractor>();

        // Act
        var state = extractor.Extract(workflowExecutionContext);
        
        // Create a new context to apply the state to
        var newWorkflowExecutionContext = await WorkflowExecutionContext.CreateAsync(
            workflowExecutionContext.ServiceProvider,
            workflowExecutionContext.WorkflowGraph,
            state.Id,
            CancellationToken.None
        );
        
        await extractor.ApplyAsync(newWorkflowExecutionContext, state);

        // Assert
        var restoredContextA = newWorkflowExecutionContext.ActivityExecutionContexts.FirstOrDefault(x => x.Id == contextA.Id);
        Assert.NotNull(restoredContextA);
        Assert.Equal(10, restoredContextA.CallStackDepth);
    }

    [Fact]
    public async Task CallStackDepth_IsIncrementedWhenSchedulingCallStackDepthProvided()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;

        var schedulingDepth = 5;
        var options = new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = "parent-activity-id",
            SchedulingWorkflowInstanceId = "parent-workflow-id",
            SchedulingCallStackDepth = schedulingDepth
        };

        // Act - Create a new activity context with scheduling information
        var context = await workflowExecutionContext.CreateActivityExecutionContextAsync(root, options);

        // Assert - The CallStackDepth should be incremented from the scheduling depth
        Assert.Equal(schedulingDepth + 1, context.CallStackDepth);
        Assert.Equal("parent-activity-id", context.SchedulingActivityExecutionId);
        Assert.Equal("parent-workflow-id", context.SchedulingWorkflowInstanceId);
    }

    [Fact]
    public async Task CallStackDepth_IsIncrementedFromParentContext()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;

        // Create a parent context with depth 3
        var parentContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(root);
        parentContext.CallStackDepth = 3;
        workflowExecutionContext.AddActivityExecutionContext(parentContext);

        var options = new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = parentContext.Id
        };

        // Act - Create a child context that references the parent
        var childContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(root, options);

        // Assert - The CallStackDepth should be parent depth + 1
        Assert.Equal(4, childContext.CallStackDepth);
        Assert.Equal(parentContext.Id, childContext.SchedulingActivityExecutionId);
    }
}
