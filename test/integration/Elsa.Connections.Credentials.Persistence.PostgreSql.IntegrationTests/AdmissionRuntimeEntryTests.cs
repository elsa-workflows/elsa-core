using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Options;
using Elsa.Workflows.Memory;
using System.Text.Json;
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
    [Theory]
    [InlineData("runtime-typed-int-long", "int-long")]
    [InlineData("runtime-typed-decimal-double", "decimal-double")]
    [InlineData("runtime-typed-json-clr", "json-clr")]
    [InlineData("runtime-typed-negative-zero", "negative-zero")]
    [InlineData("runtime-prepared-properties", "properties")]
    [InlineData("runtime-prepared-output", "output")]
    [InlineData("runtime-prepared-memory", "memory")]
    [InlineData("runtime-prepared-scheduler-input", "scheduler-input")]
    [InlineData("runtime-prepared-scheduler-depth", "scheduler-depth")]
    [InlineData("runtime-prepared-execute-delegate", "execute-delegate")]
    [InlineData("runtime-prepared-root-reference", "root-reference")]
    [InlineData("runtime-executing-notification-mutation", "executing-notification")]
    public async Task PreparedInvocationRejectsRuntimeDistinctValuesAndPlanMutation(string caseId, string scenario)
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.OnRestored = context =>
            {
                context.Input["TypedProof"] = scenario switch
                {
                    "decimal-double" => 1m,
                    "json-clr" => JsonSerializer.SerializeToElement(1),
                    "negative-zero" => BitConverter.Int64BitsToDouble(long.MinValue),
                    _ => 1
                };
                context.Properties["ProofProperty"] = "before";
                context.Output["ProofOutput"] = "before";
                context.MemoryRegister.Declare(new Variable("ProofMemory", 1, "proof-memory"));
                return Task.CompletedTask;
            };
            void Mutate(WorkflowExecutionContext context)
            {
                switch (scenario)
                {
                    case "int-long": context.Input["TypedProof"] = 1L; break;
                    case "decimal-double": context.Input["TypedProof"] = 1d; break;
                    case "json-clr": context.Input["TypedProof"] = 1; break;
                    case "negative-zero": context.Input["TypedProof"] = 0d; break;
                    case "properties": context.Properties["ProofProperty"] = "after"; break;
                    case "output": context.Output["ProofOutput"] = "after"; break;
                    case "memory": context.MemoryRegister.Blocks["proof-memory"].Value = 2; break;
                    case "scheduler-input": Assert.Single(context.Scheduler.List()).Input["Injected"] = "after"; break;
                    case "scheduler-depth": Assert.Single(context.Scheduler.List()).SchedulingCallStackDepth = 7; break;
                    case "execute-delegate": context.ExecuteDelegate = _ => ValueTask.CompletedTask; break;
                    case "root-reference": context.Workflow.Root = new AdmissionRuntimeActivity(); break;
                    case "executing-notification": context.Input["TypedProof"] = 2; break;
                    default: throw new ArgumentOutOfRangeException(nameof(scenario));
                }
            }
            host.Probe.Boundary = boundary =>
            {
                if (scenario != "executing-notification" && boundary == nameof(AdmissionExecutionBoundary.StartAuthorized))
                {
                    Mutate(host.Probe.PreparedContext!);
                }
                return Task.CompletedTask;
            };
            if (scenario == "executing-notification")
            {
                host.Probe.OnExecuting = context =>
                {
                    Mutate(context);
                    return Task.CompletedTask;
                };
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            var expectedNotifications = scenario == "executing-notification" ? 1 : 0;
            Assert.Equal(expectedNotifications, host.Probe.Count("workflowExecuting"));
            Assert.Equal(expectedNotifications, host.Probe.Count("workflowStarted"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            Assert.Equal(0, host.Probe.Count("instanceWriteAttempts"));
            Assert.Null(host.Probe.PreparedContext!.Exception);
            Assert.Equal(AdmissionState.RecoveryRequired, (await host.Store.FindAsync(host.AdmissionId))!.State);
            await AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + nameof(PreparedInvocationRejectsRuntimeDistinctValuesAndPlanMutation), caseId, [],
                new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                new Dictionary<string, object> { ["workflowExecuting"] = expectedNotifications, ["workflowStarted"] = expectedNotifications,
                    ["activityEffects"] = 0, ["instanceWriteAttempts"] = 0, ["businessFaultRecorded"] = false, ["recoveryRequired"] = true });
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
