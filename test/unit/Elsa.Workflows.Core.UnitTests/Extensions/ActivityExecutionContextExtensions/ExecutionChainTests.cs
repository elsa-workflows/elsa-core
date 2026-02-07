using Elsa.Common;
using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;

namespace Elsa.Workflows.Core.UnitTests.Extensions.ActivityExecutionContextExtensions;

public class ExecutionChainTests
{
    [Fact]
    public async Task GetExecutionChain_PreventsInfiniteLoop_WhenCircularReferenceExists()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var w = contextRoot.WorkflowExecutionContext;
        var clock = contextRoot.GetRequiredService<ISystemClock>();
        
        var contextA = new ActivityExecutionContext("A", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default);
        var contextB = new ActivityExecutionContext("B", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default);

        // Create a cycle: A -> B -> A
        contextA.SchedulingActivityExecutionId = "B";
        contextB.SchedulingActivityExecutionId = "A";

        // Register contexts in the same workflow execution context
        w.AddActivityExecutionContext(contextA);
        w.AddActivityExecutionContext(contextB);

        // Act
        var chain = contextA.GetExecutionChain().ToList();

        // Assert
        Assert.Equal(2, chain.Count);
        Assert.Equal("B", chain[0].Id);
        Assert.Equal("A", chain[1].Id);
    }
    
    [Fact]
    public async Task GetExecutionChain_ReturnsFullChain_WhenNoCycles()
    {
        // Arrange
        var root = new WriteLine("root");
        var fixture = new ActivityTestFixture(root);
        var contextRoot = await fixture.BuildAsync();
        var w = contextRoot.WorkflowExecutionContext;
        var clock = contextRoot.GetRequiredService<ISystemClock>();
        
        var contextA = new ActivityExecutionContext("A", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default);
        var contextB = new ActivityExecutionContext("B", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default);
        var contextC = new ActivityExecutionContext("C", w, null, root, contextRoot.ActivityDescriptor, contextRoot.StartedAt, null, clock, default);

        // Chain: A -> B -> C (A is leaf, C is root)
        contextA.SchedulingActivityExecutionId = "B";
        contextB.SchedulingActivityExecutionId = "C";
        contextC.SchedulingActivityExecutionId = null;

        // Register contexts in the same workflow execution context
        w.AddActivityExecutionContext(contextA);
        w.AddActivityExecutionContext(contextB);
        w.AddActivityExecutionContext(contextC);

        // Act
        var chain = contextA.GetExecutionChain().ToList();

        // Assert
        Assert.Equal(3, chain.Count);
        Assert.Equal("C", chain[0].Id);
        Assert.Equal("B", chain[1].Id);
        Assert.Equal("A", chain[2].Id);
    }
}
