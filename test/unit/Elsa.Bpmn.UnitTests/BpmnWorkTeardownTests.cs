using Elsa.Bpmn.Hosting;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// Covers the one refusal <see cref="BpmnWorkTeardown"/> can make: a subtree that still has a scheduled-but-not-yet-
/// invoked activity in it. <see cref="IActivityScheduler"/> offers no way to withdraw a queued work item, so running
/// it after BPMN destroyed its branch would leave a stray live branch behind. The host must throw rather than let
/// that happen silently.
/// </summary>
public class BpmnWorkTeardownTests
{
    [Fact]
    public async Task CancelSubtreeAsync_ThrowsNamingElementAndStrandedActivity_WhenSubtreeHasWorkStillQueued()
    {
        // A three-level activity tree, built as the actual workflow definition, so every context created below has a
        // real ActivityNode: the leaf is the work BPMN scheduled but the engine has not invoked yet, its container is
        // the subtree an interrupting boundary event is tearing down, and the root anchors the workflow.
        var queuedActivity = new WriteLine("queued") { Id = "queued-activity" };
        var subtreeActivity = new Sequence { Id = "subtree-activity", Activities = { queuedActivity } };
        var root = new Sequence { Id = "root-activity", Activities = { subtreeActivity } };

        var fixture = new ActivityTestFixture(root);

        // The fixture's default IIdentityGenerator is an unconfigured substitute that hands out the same (null)
        // id to every context; each context created below needs its own real one.
        fixture.ConfigureServices(services => services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>());

        var rootContext = await fixture.BuildAsync();
        var workflowExecutionContext = rootContext.WorkflowExecutionContext;

        // Only the root's own type is registered by the fixture; the nested activities need registering too so
        // their descriptors can be resolved when their contexts are created below.
        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(WriteLine));

        // The subtree root being torn down: already running, standing in for the BPMN unit of work a boundary event
        // just interrupted.
        var subtreeContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(subtreeActivity, new ActivityInvocationOptions { Owner = rootContext });
        workflowExecutionContext.AddActivityExecutionContext(subtreeContext);
        subtreeContext.TransitionTo(ActivityStatus.Running);

        // A child of that subtree BPMN has already scheduled but the engine has not yet invoked: its context exists
        // and is still Pending, and a matching work item sits in the scheduler.
        var queuedContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(queuedActivity, new ActivityInvocationOptions { Owner = subtreeContext });
        workflowExecutionContext.AddActivityExecutionContext(queuedContext);
        workflowExecutionContext.Scheduler.Schedule(new ActivityWorkItem(queuedActivity, existingActivityExecutionContext: queuedContext));

        var exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => BpmnWorkTeardown.CancelSubtreeAsync(subtreeContext, "boundary interrupted").AsTask());

        Assert.Contains(subtreeActivity.Id, exception.Message);
        Assert.Contains(queuedActivity.Id, exception.Message);
    }
}
