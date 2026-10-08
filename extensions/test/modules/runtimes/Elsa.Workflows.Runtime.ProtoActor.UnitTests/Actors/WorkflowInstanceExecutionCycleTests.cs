using Elsa.Extensions;
using Elsa.Testing.Shared;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.ProtoActor.UnitTests.Fixtures;
using Elsa.Workflows.State;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Proto.Cluster;
using Xunit.Abstractions;
using ProtoRunWorkflowInstanceResponse = Elsa.Workflows.Runtime.ProtoActor.ProtoBuf.RunWorkflowInstanceResponse;

namespace Elsa.Workflows.Runtime.ProtoActor.UnitTests.Actors;

public class WorkflowInstanceExecutionCycleTests(ITestOutputHelper output)
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task ActorRetainsExecutionCycleThroughTrailingSave(bool cancel, bool failSave, bool forceDrain)
    {
        ExecutionCycleHandle? pipelineHandle = null;
        await using var services = (ServiceProvider)new TestApplicationBuilder(output)
            .ConfigureElsa(elsa => elsa
                .UseWorkflowRuntime(runtime => runtime.ConfigureGracefulShutdown(options => options.DrainDeadline = TimeSpan.FromSeconds(30)))
                .UseWorkflows(workflows => workflows.WithWorkflowExecutionPipeline(pipeline => pipeline
                    .UseDefaultPipeline()
                    .Insert(1, next => async context =>
                    {
                        pipelineHandle = Assert.Single(context.GetRequiredService<IExecutionCycleRegistry>().ListActiveCycles());
                        await next(context);
                    }))))
            .Build();
        await services.PopulateRegistriesAsync();
        var registry = services.GetRequiredService<IExecutionCycleRegistry>();
        await using var harness = new WorkflowInstanceTestHarness(services.GetRequiredService<IServiceScopeFactory>());
        var workflow = Workflow.FromActivity(new WriteLine("Actor execution"));
        workflow.Identity = new(WorkflowInstanceTestHarness.DefinitionId, 1, WorkflowInstanceTestHarness.DefinitionVersionId);
        harness.WorkflowGraph = await services.GetRequiredService<IWorkflowGraphBuilder>().BuildAsync(workflow);
        var context = await WorkflowExecutionContext.CreateAsync(services, harness.WorkflowGraph, "instance");
        harness.SetupExistingInstance(services.GetRequiredService<IWorkflowStateExtractor>().Extract(context));
        var saving = new TaskCompletionSource<WorkflowState>(TaskCreationOptions.RunContinuationsAsynchronously);
        var continueSave = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var failure = failSave ? new InvalidOperationException("Actor trailing save failed.") : null;
        harness.WorkflowInstanceManager
            .Setup(x => x.SaveAsync(It.IsAny<WorkflowState>(), It.IsAny<CancellationToken>()))
            .Returns(async (WorkflowState state, CancellationToken _) =>
            {
                saving.SetResult(state);
                await continueSave.Task;
                if (failure != null)
                {
                    throw failure;
                }
                return WorkflowInstanceTestHarness.CreateWorkflowInstance(state);
            });

        using var timeout = new CancellationTokenSource(TestTimeout);
        var operation = cancel ? harness.CancelAsync(timeout.Token) : harness.RunAsync(timeout.Token);
        Task<DrainOutcome>? drain = null;
        ExecutionCycleHandle handle;
        object response;
        try
        {
            await saving.Task.WaitAsync(TestTimeout);
            handle = Assert.Single(registry.ListActiveCycles());
            Assert.Same(pipelineHandle, handle);
            Assert.False(handle.Disposed.IsCompleted);
            Assert.False(operation.IsCompleted);
            var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellationRegistration = handle.CancellationToken.Register(() => cancelled.TrySetResult());
            drain = services.GetRequiredService<IDrainOrchestrator>()
                .DrainAsync(forceDrain ? DrainTrigger.OperatorForce : DrainTrigger.HostStopSignal).AsTask();
            if (forceDrain)
            {
                await cancelled.Task.WaitAsync(TestTimeout);
            }
            Assert.False(drain.IsCompleted);
            Assert.Same(handle, Assert.Single(registry.ListActiveCycles()));
            Assert.False(handle.Disposed.IsCompleted);
        }
        finally
        {
            continueSave.TrySetResult();
            response = await operation.WaitAsync(TestTimeout);
            if (drain != null)
            {
                await drain.WaitAsync(TestTimeout);
            }
        }

        if (failSave)
        {
            Assert.Contains(failure!.Message, Assert.IsType<GrainErrorResponse>(response).Err);
        }
        else if (cancel)
        {
            Assert.IsType<GrainResponseMessage>(response);
        }
        else
        {
            Assert.IsType<ProtoRunWorkflowInstanceResponse>(response);
        }
        var outcome = await drain!;
        Assert.Equal(forceDrain ? DrainResult.Forced : DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        Assert.Equal(forceDrain ? 1 : 0, outcome.ExecutionCyclesForceCancelledCount);
        Assert.True(handle.Disposed.IsCompletedSuccessfully);
        Assert.Equal(0, registry.ActiveCount);
        harness.WorkflowInstanceManager.Verify(x => x.SaveAsync(It.IsAny<WorkflowState>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
