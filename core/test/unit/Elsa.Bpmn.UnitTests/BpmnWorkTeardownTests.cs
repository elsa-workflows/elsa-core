using Bpmn.Semantics;
using Elsa.Bpmn.Activities;
using Elsa.Bpmn.Hosting;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// Covers the case <see cref="BpmnWorkTeardown"/> used to refuse: a subtree that still has a scheduled-but-not-yet-
/// invoked activity in it. Cancelling now withdraws that work item, so the activity BPMN destroyed the branch of does
/// not run afterwards. What remains this host's own contribution is the ledger removal, persisted before the teardown
/// so that a completion arriving for torn-down work is discarded rather than fed to the interpreter as real work.
/// </summary>
public class BpmnWorkTeardownTests
{
    private const string SubtreeActivityId = "subtree-activity";
    private const string QueuedActivityId = "queued-activity";

    [Fact]
    public async Task CancelSubtreeAsync_WithdrawsTheQueuedWork_WhenSubtreeHasWorkStillQueued()
    {
        var (_, subtreeContext, _) = await BuildSubtreeWithQueuedWorkAsync();
        var workflowExecutionContext = subtreeContext.WorkflowExecutionContext;
        var queuedContext = workflowExecutionContext.ActivityExecutionContexts.Single(x => x.Activity.Id == QueuedActivityId);

        await BpmnWorkTeardown.CancelSubtreeAsync(subtreeContext, "boundary interrupted");

        // Nothing is left for the engine to take, so the descendant never gets invoked...
        Assert.False(workflowExecutionContext.Scheduler.HasAny);

        // ...and it is terminal rather than left Pending, so nothing can schedule it again either.
        Assert.Equal(ActivityStatus.Canceled, queuedContext.Status);
        Assert.Equal(ActivityStatus.Canceled, subtreeContext.Status);
    }

    [Fact]
    public async Task CancelSubtreeAsync_LeavesUnrelatedWorkScheduled_WhenSubtreeHasWorkStillQueued()
    {
        var (scopeContext, subtreeContext, _) = await BuildSubtreeWithQueuedWorkAsync();
        var workflowExecutionContext = subtreeContext.WorkflowExecutionContext;
        var siblingWorkItem = new ActivityWorkItem(new WriteLine("sibling") { Id = "sibling-activity" }, scopeContext);
        workflowExecutionContext.Scheduler.Schedule(siblingWorkItem);

        await BpmnWorkTeardown.CancelSubtreeAsync(subtreeContext, "boundary interrupted");

        Assert.Equal([siblingWorkItem], workflowExecutionContext.Scheduler.List());
    }

    [Fact]
    public async Task ApplyAsync_RemovesLedgerRecord_WhenTearingDownWorkWithSomethingStillQueued()
    {
        var (scopeContext, subtreeContext, process) = await BuildSubtreeWithQueuedWorkAsync();
        var memory = SeedLedgerRecord(scopeContext, subtreeContext);
        var applier = new BpmnCommandApplier(scopeContext, process, memory);

        await applier.ApplyAsync([new BpmnHostCommand.CancelWorkSubtree("work-1", SubtreeActivityId, "boundary interrupted")]);

        // The ledger property is reloaded from scratch, from what was actually persisted onto the context, rather
        // than read off the in-memory `memory` instance the applier already mutated.
        var reloaded = BpmnScopeMemory.Load(scopeContext);
        Assert.Null(reloaded.Work.FindByChildContextId(subtreeContext.Id));
    }

    [Fact]
    public async Task OnWorkCompletedAsync_DiscardsTheCallback_ForContextThatWasTornDown()
    {
        var (scopeContext, subtreeContext, process) = await BuildSubtreeWithQueuedWorkAsync();
        var memory = SeedLedgerRecord(scopeContext, subtreeContext);
        var applier = new BpmnCommandApplier(scopeContext, process, memory);

        await applier.ApplyAsync([new BpmnHostCommand.CancelWorkSubtree("work-1", SubtreeActivityId, "boundary interrupted")]);

        // `process.Process` is deliberately left unset. A completion that is fed to the interpreter rather than
        // discarded reaches BpmnScopeHost.Graph, which throws InvalidOperationException for want of a process
        // definition. So this call succeeding is itself the assertion: the callback for the torn-down context must
        // never get that far.
        await BpmnScopeHost.For(scopeContext).OnWorkCompletedAsync(subtreeContext, null);
    }

    /// <summary>
    /// A three-level activity tree, built as the actual workflow definition, so every context created below has a
    /// real ActivityNode: the scope is the BPMN process the ledger belongs to, the subtree is the unit of work being
    /// torn down, and the queued activity is the work BPMN scheduled but the engine has not invoked yet.
    /// </summary>
    private static async Task<(ActivityExecutionContext ScopeContext, ActivityExecutionContext SubtreeContext, BpmnProcess Process)> BuildSubtreeWithQueuedWorkAsync()
    {
        var queuedActivity = new WriteLine("queued") { Id = QueuedActivityId };
        var subtreeActivity = new Sequence { Id = SubtreeActivityId, Activities = { queuedActivity } };
        var process = new BpmnProcess { Id = "process-activity", Activities = { subtreeActivity } };

        var fixture = new ActivityTestFixture(process);

        // The fixture's default IIdentityGenerator is an unconfigured substitute that hands out the same (null)
        // id to every context; each context created below needs its own real one.
        fixture.ConfigureServices(services => services.AddSingleton<IIdentityGenerator, GuidIdentityGenerator>());

        var scopeContext = await fixture.BuildAsync();
        var workflowExecutionContext = scopeContext.WorkflowExecutionContext;

        // Only the root's own type is registered by the fixture; the nested activities need registering too so
        // their descriptors can be resolved when their contexts are created below.
        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(Sequence));
        await workflowExecutionContext.ActivityRegistry.RegisterAsync(typeof(WriteLine));

        // The subtree root being torn down: already running, standing in for the BPMN unit of work a boundary event
        // just interrupted.
        var subtreeContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(subtreeActivity, new ActivityInvocationOptions { Owner = scopeContext });
        workflowExecutionContext.AddActivityExecutionContext(subtreeContext);
        subtreeContext.TransitionTo(ActivityStatus.Running);

        // A child of that subtree BPMN has already scheduled but the engine has not yet invoked: its context exists
        // and is still Pending, and a matching work item sits in the scheduler.
        var queuedContext = await workflowExecutionContext.CreateActivityExecutionContextAsync(queuedActivity, new ActivityInvocationOptions { Owner = subtreeContext });
        workflowExecutionContext.AddActivityExecutionContext(queuedContext);
        workflowExecutionContext.Scheduler.Schedule(new ActivityWorkItem(queuedActivity, existingActivityExecutionContext: queuedContext));

        return (scopeContext, subtreeContext, process);
    }

    /// <summary>Records the subtree as the scope's live work, exactly as StartWork would have, and persists it.</summary>
    private static BpmnScopeMemory SeedLedgerRecord(ActivityExecutionContext scopeContext, ActivityExecutionContext subtreeContext)
    {
        var memory = BpmnScopeMemory.Load(scopeContext);

        memory.Work.Records.Add(new BpmnWorkRecord
        {
            Handle = "work-1",
            BindingRef = "subtree-binding",
            ElementId = SubtreeActivityId,
            ChildContextId = subtreeContext.Id
        });

        memory.SaveWork();

        return BpmnScopeMemory.Load(scopeContext);
    }
}
