using System.Data.Common;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Pipelines.ActivityExecution;
using Elsa.Workflows.Pipelines.WorkflowExecution;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.State;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeExecutionTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("runtime-completed", "completed", WorkflowSubStatus.Finished)]
    [InlineData("runtime-suspended", "suspended", WorkflowSubStatus.Suspended)]
    [InlineData("runtime-faulted", "faulted", WorkflowSubStatus.Faulted)]
    public async Task RealDefaultPipelineRecordsActualOutcomeAfterFinalWrite(string caseId, string outcome, WorkflowSubStatus expected)
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Outcome = outcome;
            var response = await host.Execution.ExecuteAsync(host.AdmissionId);
            Assert.Equal(expected, response!.SubStatus);
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(expected == WorkflowSubStatus.Suspended ? AdmissionState.ExecutionObserved : AdmissionState.Terminal, record.State);
            Assert.False(record.AuthorityOutstanding);
            Assert.NotNull(record.CheckpointFingerprint);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(1, host.Probe.Count("workflowExecuting"));
            Assert.Equal(1, host.Probe.Count("workflowStarted"));
            Assert.Equal(1, host.Probe.Count("TrailingWriteCompleted"));
            Assert.Equal(1, host.Probe.Count("OwnershipUnwound"));
            Assert.Equal(1, host.Probe.Count("CheckpointRecorded"));
            await ObserveAsync(caseId, nameof(RealDefaultPipelineRecordsActualOutcomeAfterFinalWrite), caseId,
                new() { ["activityEffects"] = 1, ["checkpointRecorded"] = true, ["subStatus"] = expected.ToString() });
        });
    }

    [Fact]
    public async Task CreationClaimCommitCannotRaceOperatorResolutionBeforeInsert()
    {
        var barrier = new CreationCommitGate();
        await WithHostAsync(async host =>
        {
            barrier.Armed = true;
            var execution = host.Execution.ExecuteAsync(host.AdmissionId);
            try
            {
                await barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
                var record = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(AdmissionState.Creating, record.State);
                Assert.Equal(0, host.Probe.Count("instanceInsertAttempts"));
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(record.Id, record.Revision,
                    AdmissionTerminalDisposition.Resolved, "fixture-owner-resolution", true, true));
                Assert.Equal(AdmissionState.Creating, (await host.Store.FindAsync(record.Id))!.State);
            }
            finally
            {
                barrier.Release.TrySetResult();
                await execution;
            }
            Assert.Equal(1, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-creation-owner-race", nameof(CreationClaimCommitCannotRaceOperatorResolutionBeforeInsert), "default",
                new() { ["resolutionDenied"] = true, ["instanceInsertAttempts"] = 1, ["activityEffects"] = 1 });
        }, barrier);
    }

    [Theory]
    [InlineData("runtime-graph-parent", "parent")]
    [InlineData("runtime-graph-child", "child")]
    public async Task PreparedGraphRelationshipMutationDeniesBeforeExecution(string caseId, string scenario)
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Boundary = boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.StartAuthorized))
                {
                    var nodes = host.Probe.PreparedContext!.WorkflowGraph.Nodes.ToArray();
                    Assert.True(nodes.Length >= 2);
                    if (scenario == "parent")
                    {
                        nodes[^1].AddParent(nodes[0]);
                    }
                    else
                    {
                        nodes[0].AddChild(nodes[^1]);
                    }
                }
                return Task.CompletedTask;
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("workflowStarted"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            Assert.Equal(AdmissionState.RecoveryRequired, (await host.Store.FindAsync(host.AdmissionId))!.State);
            await ObserveAsync(caseId, nameof(PreparedGraphRelationshipMutationDeniesBeforeExecution), caseId,
                new() { ["workflowExecuting"] = 0, ["activityEffects"] = 0, ["recoveryRequired"] = true });
        });
    }

    [Fact]
    public async Task SameMethodNameDifferentCompletionTargetCannotEscapeRevalidation()
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var initial = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            var bookmark = Assert.Single(initial.Bookmarks);
            var replaced = false;
            var first = new CompletionTarget();
            var second = new CompletionTarget();
            ActivityCompletionCallback firstCallback = first.Complete;
            ActivityCompletionCallback secondCallback = second.Complete;
            Assert.Equal(firstCallback.Method.Name, secondCallback.Method.Name);
            host.Probe.OnRestored = context =>
            {
                var entry = Assert.Single(context.CompletionCallbacks);
                context.RemoveCompletionCallback(entry);
                context.AddCompletionCallback(entry.Owner, entry.Child, firstCallback, entry.Tag);
                return Task.CompletedTask;
            };
            host.Probe.Boundary = boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.StartAuthorized))
                {
                    var context = host.Probe.PreparedContext!;
                    var entry = Assert.Single(context.CompletionCallbacks);
                    Assert.Same(firstCallback, entry.CompletionCallback);
                    // Same serialized method name, owner, child and tag, but a different target.
                    context.RemoveCompletionCallback(entry);
                    context.AddCompletionCallback(entry.Owner, entry.Child, secondCallback, entry.Tag);
                    replaced = true;
                }
                return Task.CompletedTask;
            };
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(initial.WorkflowInstanceId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id }));
            Assert.True(replaced);
            Assert.Equal(1, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("activityResumes"));
            Assert.Equal(AdmissionState.RecoveryRequired, (await host.Store.FindAsync(host.AdmissionId))!.State);
            await ObserveAsync("runtime-completion-target", nameof(SameMethodNameDifferentCompletionTargetCannotEscapeRevalidation), "default",
                new() { ["callbackReplaced"] = true, ["activityResumes"] = 0, ["recoveryRequired"] = true });
        });
    }

    [Fact]
    public async Task FrozenCompositionsRejectSetupFromAuthorizationAndExecutingCallbacks()
    {
        await WithHostAsync(async host =>
        {
            var setupCallbacks = 0;
            var workflow = (WorkflowExecutionPipeline)host.Services.GetRequiredService<IWorkflowExecutionPipeline>();
            var activity = (ActivityExecutionPipeline)host.Services.GetRequiredService<IActivityExecutionPipeline>();
            void AttemptSetup()
            {
                Assert.Throws<InvalidOperationException>(() => workflow.Setup(builder => { setupCallbacks++; builder.Reset(); }));
                Assert.Throws<InvalidOperationException>(() => activity.Setup(builder => { setupCallbacks++; builder.Reset(); }));
            }
            host.Probe.Boundary = boundary =>
            {
                if (boundary is nameof(AdmissionExecutionBoundary.StartAuthorized) or nameof(AdmissionExecutionBoundary.AuthorityConsumed) or nameof(AdmissionExecutionBoundary.BeforeRunnerEntry))
                {
                    AttemptSetup();
                }
                return Task.CompletedTask;
            };
            host.Probe.OnExecuting = _ => { AttemptSetup(); return Task.CompletedTask; };
            Assert.Equal(WorkflowSubStatus.Finished, (await host.Execution.ExecuteAsync(host.AdmissionId))!.SubStatus);
            Assert.Equal(0, setupCallbacks);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-frozen-compositions", nameof(FrozenCompositionsRejectSetupFromAuthorizationAndExecutingCallbacks), "default",
                new() { ["setupCallbacks"] = 0, ["activityEffects"] = 1 });
        });
    }

    [Fact]
    public async Task PublicActivityEntriesRemainDeniedDuringAuthorizedRunAndAfterUnwind()
    {
        await WithHostAsync(async host =>
        {
            var reached = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == "ActivityEffect")
                {
                    reached.TrySetResult();
                    await release.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
            };
            var run = host.Execution.ExecuteAsync(host.AdmissionId);
            ActivityExecutionContext? retained = null;
            Func<Task>? cached = null;
            try
            {
                await reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
                var context = host.Probe.PreparedContext!;
                retained = context.ActivityExecutionContexts.Single(x => x.Activity is AdmissionRuntimeActivity);
                var pipeline = host.Services.GetRequiredService<IActivityExecutionPipeline>();
                var invoker = host.Services.GetRequiredService<IActivityInvoker>();
                var builder = new ActivityExecutionPipelinePipelineBuilder(host.Services);
                var built = builder.Use(next => next).Build();
                cached = () => built(retained).AsTask();
                await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync(retained));
                await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Pipeline(retained).AsTask());
                await Assert.ThrowsAsync<InvalidOperationException>(cached);
                await Assert.ThrowsAsync<InvalidOperationException>(() => invoker.InvokeAsync(retained));
                await Assert.ThrowsAsync<InvalidOperationException>(async () => { await invoker.InvokeAsync(context, new AdmissionRuntimeActivity()); });
                Assert.Equal(1, host.Probe.Count("activityEffects"));
            }
            finally
            {
                release.TrySetResult();
                await run;
            }
            await Assert.ThrowsAsync<InvalidOperationException>(cached!);
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Services.GetRequiredService<IActivityInvoker>().InvokeAsync(retained!));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-public-activity-denial", nameof(PublicActivityEntriesRemainDeniedDuringAuthorizedRunAndAfterUnwind), "default",
                new() { ["activityEffects"] = 1, ["duringExecutionDenied"] = true, ["afterUnwindDenied"] = true });
        });
    }

    [Fact]
    public async Task LegacyBookmarkUpsertCannotReplaceOwnedRowWithForgedUnownedIdentity()
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var initial = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            var bookmark = Assert.Single(initial.Bookmarks);
            var store = host.Services.GetRequiredService<IBookmarkStore>();
            var serializer = host.Services.GetRequiredService<IPayloadSerializer>();
            var original = (await store.FindAsync(new BookmarkFilter { BookmarkId = bookmark.Id }))!;
            var before = serializer.Serialize(original);
            var saves = host.Probe.Count("bookmarkSaveCalls");
            var forged = new StoredBookmark
            {
                Id = original.Id, TenantId = "foreign-forged-tenant", WorkflowInstanceId = "definitively-unowned-instance",
                ActivityInstanceId = original.ActivityInstanceId, Name = "forged-bookmark", Hash = "forged-hash", CreatedAt = original.CreatedAt
            };
            var runtime = host.Services.GetRequiredService<IWorkflowRuntime>();
#pragma warning disable CS0618 // Exercise the supported legacy facade's actual write boundary.
            await Assert.ThrowsAsync<InvalidOperationException>(() => runtime.UpdateBookmarkAsync(forged));
#pragma warning restore CS0618
            Assert.Equal(saves, host.Probe.Count("bookmarkSaveCalls"));
            Assert.Equal(before, serializer.Serialize((await store.FindAsync(new BookmarkFilter { BookmarkId = bookmark.Id }))!));
            await ObserveAsync("runtime-bookmark-owner-upsert", nameof(LegacyBookmarkUpsertCannotReplaceOwnedRowWithForgedUnownedIdentity), "default",
                new() { ["bookmarkSaveDelta"] = 0, ["originalUnchanged"] = true });
        });
    }

    [Fact]
    public async Task MutablePreparedContextIdentityCannotEmitCancellationNotification()
    {
        await WithHostAsync(async host =>
        {
            var denied = false;
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.BeforeRunnerEntry))
                {
                    var context = host.Probe.PreparedContext!;
                    var original = context.Id;
                    try
                    {
                        context.Id = "definitively-unowned-instance";
                        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Services.GetRequiredService<IWorkflowCanceler>().CancelWorkflowAsync(context));
                        denied = true;
                    }
                    finally
                    {
                        context.Id = original;
                    }
                }
            };
            await host.Execution.ExecuteAsync(host.AdmissionId);
            Assert.True(denied);
            Assert.Equal(0, host.Probe.Count("workflowCancelling"));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-cancel-context-identity", nameof(MutablePreparedContextIdentityCannotEmitCancellationNotification), "default",
                new() { ["workflowCancelling"] = 0, ["activityEffects"] = 1 });
        });
    }

    [Theory]
    [InlineData("runtime-output-included", "included", true)]
    [InlineData("runtime-output-omitted", "omitted", false)]
    public async Task LegitimateLocalClientContinuationReturnsRequestedDetachedPersistedOutput(string caseId, string scenario, bool includeOutput)
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var initial = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(initial.WorkflowInstanceId);
            var response = await client.RunInstanceAsync(new RunWorkflowInstanceRequest
            {
                BookmarkId = Assert.Single(initial.Bookmarks).Id, IncludeWorkflowOutput = includeOutput
            });
            Assert.Equal(WorkflowSubStatus.Finished, response.SubStatus);
            if (includeOutput)
            {
                Assert.Equal("persisted-resume-output", response.Output!["Proof"]);
                response.Output["Proof"] = "caller-mutated-output";
                var persisted = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(initial.WorkflowInstanceId))!;
                Assert.Equal("persisted-resume-output", persisted.WorkflowState.Output["Proof"]);
            }
            else
            {
                Assert.Null(response.Output);
            }
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(1, host.Probe.Count("activityResumes"));
            await ObserveAsync(caseId, nameof(LegitimateLocalClientContinuationReturnsRequestedDetachedPersistedOutput), caseId,
                new() { ["activityEffects"] = 1, ["activityResumes"] = 1, ["outputIncluded"] = includeOutput, ["persistedOutputUnchanged"] = true });
        });
    }

    [Theory]
    [InlineData("runtime-input-order", "order")]
    [InlineData("runtime-input-comparer", "comparer")]
    public async Task PreparedRuntimeDictionarySemanticsCannotChangeBeforeConsumption(string caseId, string scenario)
    {
        await WithHostAsync(async host =>
        {
            host.Probe.Boundary = boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.StartAuthorized))
                {
                    var context = host.Probe.PreparedContext!;
                    var original = Assert.IsType<Dictionary<string, object>>(context.Input);
                    Assert.True(original.Count >= 2);
                    context.Input = scenario == "order"
                        ? new Dictionary<string, object>(original.Reverse(), original.Comparer)
                        : new Dictionary<string, object>(original, StringComparer.Ordinal);
                }
                return Task.CompletedTask;
            };
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            Assert.Equal(AdmissionState.RecoveryRequired, (await host.Store.FindAsync(host.AdmissionId))!.State);
            await ObserveAsync(caseId, nameof(PreparedRuntimeDictionarySemanticsCannotChangeBeforeConsumption), caseId,
                new() { ["workflowExecuting"] = 0, ["activityEffects"] = 0, ["recoveryRequired"] = true });
        });
    }

    private async Task WithHostAsync(Func<RuntimeScenario, Task> assertion, IInterceptor? interceptor = null)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, admissionInterceptor: interceptor);
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await AdmissionRuntimeHost.MigrateAsync(services);
        await AdmissionRuntimeHost.BootstrapAsync(services);
        var execution = services.GetRequiredService<AdmissionExecutionService>();
        var admission = await execution.AdmitAsync(AdmissionWorkerHost.Event());
        Assert.Equal(AdmissionOutcome.Committed, admission.Outcome);
        await assertion(new(services, probe, execution, services.GetRequiredService<IAdmissionStore>(), admission.AdmissionId!));
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeExecutionTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);

    private sealed record RuntimeScenario(IServiceProvider Services, AdmissionRuntimeProbe Probe, AdmissionExecutionService Execution, IAdmissionStore Store, string AdmissionId);
    private sealed class CompletionTarget
    {
        public ValueTask Complete(ActivityCompletedContext context) => ValueTask.CompletedTask;
    }
    private sealed class CreationCommitGate : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed)
            {
                Armed = false;
                Reached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }
    }
}
