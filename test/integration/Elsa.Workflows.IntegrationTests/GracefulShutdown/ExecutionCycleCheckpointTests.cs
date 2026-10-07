using Elsa.Common;
using Elsa.Extensions;
using Elsa.Mediator.Contracts;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.CommitStates;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.Runtime;
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
        await using var services = (ServiceProvider)new TestApplicationBuilder(output)
            .AddActivitiesFrom<DrainLiveActivity>()
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options => options.DrainDeadline = TimeSpan.FromSeconds(30)))
                .UseWorkflows(workflows =>
                {
                    if (checkpoint == "workflow-before")
                        workflows.WithDefaultWorkflowCommitStrategy(new WorkflowExecutingWorkflowStrategy());
                    else if (checkpoint == "activity-before")
                        workflows.WithDefaultActivityCommitStrategy(new ExecutingActivityStrategy());
                    else if (checkpoint == "activity-after")
                        workflows.WithDefaultActivityCommitStrategy(new ExecutedActivityStrategy());
                }))
            .Build();
        await services.PopulateRegistriesAsync();
        var registry = services.GetRequiredService<IExecutionCycleRegistry>();
        var downstream = new DrainLiveGate();
        var finalWrite = new DrainLiveGate();
        var probe = new CheckpointProbe { ExplicitCommit = checkpoint == "explicit" };
        var workflow = Workflow.FromActivity(new Sequence { Activities = { probe, new DrainLiveActivity { Gate = downstream } } });
        var runner = new WorkflowRunner(services,
            services.GetRequiredService<IWorkflowExecutionPipeline>(), services.GetRequiredService<IWorkflowStateExtractor>(),
            services.GetRequiredService<IWorkflowBuilderFactory>(), services.GetRequiredService<IWorkflowGraphBuilder>(),
            services.GetRequiredService<IIdentityGenerator>(), services.GetRequiredService<INotificationSender>(),
            new WorkflowLoggerStateGenerator(), new GatedFinalCommit(services.GetRequiredService<ICommitStateHandler>(), finalWrite),
            services.GetRequiredService<ILogger<WorkflowRunner>>());
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
        public Task CommitAsync(WorkflowExecutionContext context, CancellationToken cancellationToken = default) => inner.CommitAsync(context, cancellationToken);

        public async Task CommitAsync(WorkflowExecutionContext context, WorkflowState state, CancellationToken cancellationToken = default)
        {
            gate.Started.SetResult();
            await gate.Continue.Task;
            await inner.CommitAsync(context, state, cancellationToken);
        }
    }
}
