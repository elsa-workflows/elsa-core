using Elsa.Common;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Activities;
using Elsa.Workflows.Services;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Elsa.Workflows.IntegrationTests.GracefulShutdown;

public class ExecutionCycleCheckpointTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("workflow-before", false)]
    [InlineData("activity-before", false)]
    [InlineData("activity-after", false)]
    [InlineData("explicit", false)]
    [InlineData("explicit", true)]
    public async Task CheckpointRetainsHandleThroughDownstreamActivityAndFinalWrite(string checkpoint, bool forceDrain)
    {
        await using var services = BuildServices(checkpoint);
        await services.PopulateRegistriesAsync();
        var registry = services.GetRequiredService<IExecutionCycleRegistry>();
        var downstream = new DrainLiveGate();
        var finalWrite = new DrainLiveGate();
        var probe = new CheckpointProbe { ExplicitCommit = checkpoint == "explicit" };
        var workflow = Workflow.FromActivity(new Sequence { Activities = { probe, new DrainLiveActivity { Gate = downstream } } });
        var commit = new GatedFinalCommit(services.GetRequiredService<ICommitStateHandler>(), finalWrite);
        var runner = CreateRunner(services, commit);
        var run = runner.RunAsync(workflow);
        Task<DrainOutcome>? drain = null;
        try
        {
            await downstream.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var handle = Assert.Single(registry.ListActiveCycles());
            Assert.Same(probe.Handle, handle);
            Assert.False(handle.Disposed.IsCompleted);
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellationRegistration = handle.CancellationToken.Register(() => cancelled.TrySetResult());
            var trigger = forceDrain ? DrainTrigger.OperatorForce : DrainTrigger.HostStopSignal;
            drain = services.GetRequiredService<IDrainOrchestrator>().DrainAsync(trigger).AsTask();
            if (forceDrain)
            {
                // Observe force drain finding and cancelling the same post-checkpoint handle before releasing work.
                await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));
            }
            Assert.False(drain.IsCompleted);

            downstream.Continue.SetResult();
            await finalWrite.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Same(handle, Assert.Single(registry.ListActiveCycles()));
            Assert.Same(handle, commit.Handle);
            Assert.False(handle.Disposed.IsCompleted);
            Assert.False(drain.IsCompleted);
        }
        finally
        {
            downstream.Continue.TrySetResult();
            finalWrite.Continue.TrySetResult();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        var outcome = await drain!.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(forceDrain ? DrainResult.Forced : DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        Assert.Equal(forceDrain ? 1 : 0, outcome.ExecutionCyclesForceCancelledCount);
        Assert.Equal(0, registry.ActiveCount);
    }

    [Theory]
    [InlineData(WorkflowSubStatus.Finished)]
    [InlineData(WorkflowSubStatus.Suspended)]
    [InlineData(WorkflowSubStatus.Faulted)]
    public async Task RealWorkflowOutcomeRetainsHandleThroughFinalWrite(WorkflowSubStatus expectedSubStatus)
    {
        await using var services = BuildServices();
        await services.PopulateRegistriesAsync();
        var registry = services.GetRequiredService<IExecutionCycleRegistry>();
        var finalWrite = new DrainLiveGate();
        var commit = new GatedFinalCommit(services.GetRequiredService<ICommitStateHandler>(), finalWrite);
        var probe = new CheckpointProbe { ExplicitCommit = true };
        IActivity outcome = expectedSubStatus switch
        {
            WorkflowSubStatus.Finished => new WriteLine("Completed"),
            WorkflowSubStatus.Suspended => new Event("execution-cycle-suspension"),
            WorkflowSubStatus.Faulted => Fault.Create("test", "ExecutionCycle", "System", "Expected workflow fault"),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedSubStatus))
        };
        var workflow = Workflow.FromActivity(new Sequence { Activities = { probe, outcome } });
        var run = CreateRunner(services, commit).RunAsync(workflow);
        ExecutionCycleHandle handle;
        try
        {
            await finalWrite.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
            handle = Assert.Single(registry.ListActiveCycles());
            Assert.Same(probe.Handle, handle);
            Assert.Same(handle, commit.Handle);
            Assert.False(handle.Disposed.IsCompleted);
            Assert.False(run.IsCompleted);
            var state = Assert.IsType<WorkflowState>(commit.State);
            Assert.Equal(expectedSubStatus, state.SubStatus);
            Assert.Equal(expectedSubStatus == WorkflowSubStatus.Suspended ? WorkflowStatus.Running : WorkflowStatus.Finished, state.Status);
            if (expectedSubStatus == WorkflowSubStatus.Suspended)
            {
                Assert.Single(state.Bookmarks);
            }
            else if (expectedSubStatus == WorkflowSubStatus.Faulted)
            {
                var incident = Assert.Single(state.Incidents);
                Assert.Equal(typeof(FaultException), incident.Exception!.Type);
                Assert.Equal("Expected workflow fault", incident.Message);
            }
        }
        finally
        {
            finalWrite.Continue.TrySetResult();
            await run.WaitAsync(TimeSpan.FromSeconds(5));
        }
        var result = await run;
        Assert.Same(commit.State, result.WorkflowState);
        Assert.Equal(expectedSubStatus, result.WorkflowState.SubStatus);
        Assert.True(handle.Disposed.IsCompleted);
        Assert.Equal(0, registry.ActiveCount);
    }

    private ServiceProvider BuildServices(string? checkpoint = null) => (ServiceProvider)new TestApplicationBuilder(output)
        .AddActivitiesFrom<DrainLiveActivity>()
        .ConfigureElsa(elsa => elsa
            .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options => options.DrainDeadline = TimeSpan.FromSeconds(30)))
            .UseWorkflows(workflows =>
            {
                workflows.WithWorkflowExecutionPipeline(pipeline => pipeline.UseDefaultPipeline());
                if (checkpoint == "workflow-before")
                {
                    workflows.WithDefaultWorkflowCommitStrategy(new WorkflowExecutingWorkflowStrategy());
                }
                else if (checkpoint == "activity-before")
                {
                    workflows.WithDefaultActivityCommitStrategy(new ExecutingActivityStrategy());
                }
                else if (checkpoint == "activity-after")
                {
                    workflows.WithDefaultActivityCommitStrategy(new ExecutedActivityStrategy());
                }
            }))
        .Build();

    private static WorkflowRunner CreateRunner(IServiceProvider services, ICommitStateHandler commit) => new(services,
        services.GetRequiredService<IWorkflowExecutionPipeline>(), services.GetRequiredService<IWorkflowStateExtractor>(),
        services.GetRequiredService<IWorkflowBuilderFactory>(), services.GetRequiredService<IWorkflowGraphBuilder>(),
        services.GetRequiredService<IIdentityGenerator>(), services.GetRequiredService<INotificationSender>(),
        new WorkflowLoggerStateGenerator(), commit, services.GetRequiredService<ILogger<WorkflowRunner>>());

    public sealed class CheckpointProbe : CodeActivity
    {
        public bool ExplicitCommit { get; set; }
        public ExecutionCycleHandle? Handle { get; private set; }

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var registry = context.GetRequiredService<IExecutionCycleRegistry>();
            Handle = Assert.Single(registry.ListActiveCycles());
            if (ExplicitCommit)
            {
                await context.WorkflowExecutionContext.CommitAsync();
                Assert.Same(Handle, Assert.Single(registry.ListActiveCycles()));
            }
        }
    }

    private sealed class GatedFinalCommit(ICommitStateHandler inner, DrainLiveGate gate) : ICommitStateHandler
    {
        public WorkflowState? State { get; private set; }
        public ExecutionCycleHandle? Handle { get; private set; }

        public Task CommitAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default) => inner.CommitAsync(context, cancellationToken);

        public async Task CommitAsync(WorkflowExecutionContext context, WorkflowState state, CancellationToken cancellationToken = default)
        {
            State = state;
            var handle = Assert.Single(context.GetRequiredService<IExecutionCycleRegistry>().ListActiveCycles());
            Handle = handle;
            gate.Started.SetResult();
            await gate.Continue.Task;
            await inner.CommitAsync(context, state, cancellationToken);
            Assert.Same(handle, Assert.Single(context.GetRequiredService<IExecutionCycleRegistry>().ListActiveCycles()));
            Assert.False(handle.Disposed.IsCompleted);
        }
    }
}
