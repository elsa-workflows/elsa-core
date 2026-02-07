using Elsa.Common;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Workflows.Core.UnitTests.Contexts;

public class WorkflowExecutionContextTests
{
    [Fact]
    public async Task CreateActivityExecutionContextAsync_CalculatesCallStackDepth_Correctly()
    {
        // Arrange
        var activityA = new WriteLine("A");
        var activityB = new WriteLine("B");
        var activityC = new WriteLine("C");
        var root = new Sequence { Activities = { activityA, activityB, activityC } };
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var workflowExecutionContext = contextRoot.WorkflowExecutionContext;

        // Act
        // Level 0 (already created by fixture.BuildAsync for the root activity, but let's create explicitly for clarity)
        var contextA = await workflowExecutionContext.CreateActivityExecutionContextAsync(activityA);
        
        // Level 1: B scheduled by A
        var contextB = await workflowExecutionContext.CreateActivityExecutionContextAsync(activityB, new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = contextA.Id
        });
        
        // Level 2: C scheduled by B
        var contextC = await workflowExecutionContext.CreateActivityExecutionContextAsync(activityC, new ActivityInvocationOptions
        {
            SchedulingActivityExecutionId = contextB.Id
        });

        // Assert
        Assert.Equal(0, contextA.CallStackDepth);
        Assert.Equal(1, contextB.CallStackDepth);
        Assert.Equal(2, contextC.CallStackDepth);
    }
}
