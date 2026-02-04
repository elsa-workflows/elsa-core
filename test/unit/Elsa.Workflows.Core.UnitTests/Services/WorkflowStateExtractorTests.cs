using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
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
}
