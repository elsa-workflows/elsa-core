using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Options;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Requests;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeEntryTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Theory]
    [InlineData("runtime-held-client", "client")]
    [InlineData("runtime-held-client-create", "client-create")]
    [InlineData("runtime-held-client-cancel", "client-cancel")]
    [InlineData("runtime-held-client-delete", "client-delete")]
    [InlineData("runtime-held-client-import", "client-import")]
    [InlineData("runtime-held-legacy-resume", "legacy-resume")]
    [InlineData("runtime-held-legacy-cancel", "legacy-cancel")]
    [InlineData("runtime-held-legacy-import", "legacy-import")]
    [InlineData("runtime-held-dispatcher", "dispatcher")]
    [InlineData("runtime-held-runner-activity", "runner-activity")]
    [InlineData("runtime-held-runner-state", "runner-state")]
    [InlineData("runtime-held-runner-context-copy", "runner-context-copy")]
    [InlineData("runtime-held-pipeline-execute", "pipeline-execute")]
    [InlineData("runtime-held-pipeline-delegate", "pipeline-delegate")]
    [InlineData("runtime-held-cached-builder", "cached-builder")]
    public async Task HeldInitialAuthorityRejectsUnboundRunnerAndPublicPipelineEntries(string caseId, string scenario)
    {
        await _runtime.RunAsync(async host =>
        {
            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var builder = new WorkflowExecutionPipelineBuilder(host.Services);
            var middlewareCalls = 0;
            builder.Use(next => async context =>
            {
                middlewareCalls++;
                await next(context);
            });
            var cached = builder.Build(); // Captured before private authority is prepared/bound.
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.BeforeRunnerEntry))
                {
                    reached.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
            };
            var execution = host.Execution.ExecuteAsync(host.AdmissionId);
            try
            {
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
                var context = host.Probe.PreparedContext!;
                var record = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(AdmissionState.StartAuthorized, record.State);
                Assert.True(record.AuthorityOutstanding);
                var runner = host.Services.GetRequiredService<IWorkflowRunner>();
                var pipeline = host.Services.GetRequiredService<IWorkflowExecutionPipeline>();
                var state = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(context.Id))!.WorkflowState;
                var runtime = host.Services.GetRequiredService<IWorkflowRuntime>();
                var client = await runtime.CreateClientAsync(context.Id);
                Func<Task> competing = scenario switch
                {
                    "client" => () => client.RunInstanceAsync(new RunWorkflowInstanceRequest()),
                    "client-create" => () => client.CreateInstanceAsync(new CreateWorkflowInstanceRequest()),
                    "client-cancel" => () => client.CancelAsync(),
                    "client-delete" => () => client.DeleteAsync(),
                    "client-import" => () => client.ImportStateAsync(state),
                    "legacy-resume" or "legacy-cancel" or "legacy-import" => () => InvokeLegacyAsync(runtime, state, scenario),
                    "dispatcher" => () => host.Services.GetRequiredService<IWorkflowDispatcher>().DispatchAsync(new DispatchWorkflowInstanceRequest(context.Id), null),
                    "runner-activity" => () => runner.RunAsync(new AdmissionRuntimeActivity(), new RunWorkflowOptions { WorkflowInstanceId = context.Id }),
                    "runner-state" => () => runner.RunAsync(context.WorkflowGraph, state),
                    "runner-context-copy" => async () =>
                    {
                        var copy = await WorkflowExecutionContext.CreateAsync(host.Services, context.WorkflowGraph, state);
                        Assert.NotSame(context, copy);
                        await runner.RunAsync(copy);
                    },
                    "pipeline-execute" => () => pipeline.ExecuteAsync(context),
                    "pipeline-delegate" => () => pipeline.Pipeline(context).AsTask(),
                    "cached-builder" => () => cached(context).AsTask(),
                    _ => throw new ArgumentOutOfRangeException(nameof(scenario))
                };
                await Assert.ThrowsAsync<InvalidOperationException>(competing);
                Assert.Equal(0, middlewareCalls);
                Assert.Equal(0, host.Probe.Count("workflowExecuting"));
                Assert.Equal(0, host.Probe.Count("workflowStarted"));
                Assert.Equal(0, host.Probe.Count("activityEffects"));
                Assert.Equal(0, host.Probe.Count("AuthorityConsumed"));
                Assert.Equal(0, host.Probe.Count("workflowCancelling"));
                Assert.Equal(0, host.Probe.Count("instanceWriteAttempts"));
                Assert.Equal(0, host.Probe.Count("bookmarkSaveCalls"));
            }
            finally
            {
                release.TrySetResult();
                await execution;
            }
            Assert.Equal(1, host.Probe.Count("workflowExecuting"));
            Assert.Equal(1, host.Probe.Count("workflowStarted"));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(1, host.Probe.Count("AuthorityConsumed"));
            await AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + nameof(HeldInitialAuthorityRejectsUnboundRunnerAndPublicPipelineEntries), caseId, [],
                new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                new Dictionary<string, object> { ["competingMiddlewareCalls"] = 0, ["competingStartNotifications"] = 0,
                    ["competingActivityEffects"] = 0, ["authorizedEntries"] = 1, ["activityEffects"] = 1 });
        });
    }
#pragma warning disable CS0618 // Exercise the actual legacy facade rather than a duplicate wrapper.
    private static async Task InvokeLegacyAsync(IWorkflowRuntime runtime, Elsa.Workflows.State.WorkflowState state, string scenario)
    {
        switch (scenario)
        {
            case "legacy-resume":
                await runtime.ResumeWorkflowAsync(state.Id);
                break;
            case "legacy-cancel":
                await runtime.CancelWorkflowAsync(state.Id);
                break;
            case "legacy-import":
                await runtime.ImportWorkflowStateAsync(state);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scenario));
        }
    }
#pragma warning restore CS0618

}
