using System.Data.Common;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Filters;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeCheckpointTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("runtime-checkpoint-observer-created", "created")]
    [InlineData("runtime-checkpoint-observer-materialized", "materialized")]
    [InlineData("runtime-checkpoint-observer-resumed", "resumed")]
    public async Task ConfirmedSuspendedCheckpointSurvivesObserverFailureAndRemainsResumable(string caseId, string scenario)
    {
        await new AdmissionRuntimeTestFixture(fixture).RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            if (scenario == "materialized")
            {
                await AdmissionRuntimeTestFixture.MaterializeAsync(host);
            }
            else if (scenario == "resumed")
            {
                Assert.Equal(WorkflowSubStatus.Suspended, (await host.Execution.ExecuteAsync(host.AdmissionId))!.SubStatus);
                host.Probe.ResuspendOnResume = true;
            }
            var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
            var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
            var payloads = host.Services.GetRequiredService<IPayloadSerializer>();
            var bookmarks = host.Services.GetRequiredService<IBookmarkStore>();
            var previous = (await host.Store.FindAsync(host.AdmissionId))!;
            var checkpoints = host.Probe.Count("CheckpointRecorded");
            var failure = new IOException("fixture_checkpoint_observer_failed");
            AdmissionRecord? confirmed = null;
            string? confirmedState = null;
            string? confirmedBookmarks = null;
            host.Probe.Boundary = async boundary =>
            {
                if (boundary != nameof(AdmissionExecutionBoundary.CheckpointRecorded))
                {
                    return;
                }
                confirmed = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(AdmissionState.ExecutionObserved, confirmed.State);
                Assert.True(confirmed.Revision > previous.Revision);
                Assert.False(confirmed.AuthorityOutstanding);
                var instance = (await instances.FindByIdAsync(confirmed.WorkflowInstanceId!))!;
                Assert.Equal(WorkflowSubStatus.Suspended, instance.SubStatus);
                Assert.Single(instance.WorkflowState.Bookmarks);
                confirmedState = serializer.Serialize(instance.WorkflowState);
                confirmedBookmarks = payloads.Serialize((await bookmarks.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = instance.Id }))
                    .OrderBy(bookmark => bookmark.Id, StringComparer.Ordinal).ToArray());
                // The execution-cycle scope unwound, but the operation's local owner is
                // still held while post-checkpoint instrumentation completes/fails.
                Assert.Equal(0, host.Services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount);
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(confirmed.Id, confirmed.Revision,
                    AdmissionTerminalDisposition.Resolved, "fixture-checkpoint-owner-held", true, true));
                throw failure;
            };
            var client = previous.WorkflowInstanceId == null ? null :
                await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(previous.WorkflowInstanceId);
            var originalState = scenario == "resumed" ? (await instances.FindByIdAsync(previous.WorkflowInstanceId!))!.WorkflowState : null;
            var thrown = await Assert.ThrowsAsync<IOException>(async () =>
            {
                if (scenario == "resumed")
                {
                    await client!.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = Assert.Single(originalState!.Bookmarks).Id });
                }
                else
                {
                    await host.Execution.ExecuteAsync(host.AdmissionId);
                }
            });
            Assert.Same(failure, thrown);
            host.Probe.Boundary = null;
            Assert.NotNull(confirmed);
            Assert.Equal(checkpoints + 1, host.Probe.Count("CheckpointRecorded"));
            var after = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(confirmed.Revision, after.Revision);
            Assert.Equal(AdmissionState.ExecutionObserved, after.State);
            Assert.Equal(confirmed.AttemptId, after.AttemptId);
            Assert.Equal(confirmed.CheckpointFingerprint, after.CheckpointFingerprint);
            Assert.Equal(confirmed.BookmarkIdsJson, after.BookmarkIdsJson);
            Assert.False(after.AuthorityOutstanding);
            Assert.Null(after.RecoveryCode);
            var persisted = (await instances.FindByIdAsync(after.WorkflowInstanceId!))!;
            Assert.Equal(confirmedState, serializer.Serialize(persisted.WorkflowState));
            Assert.Equal(confirmedBookmarks, payloads.Serialize((await bookmarks.FindManyAsync(new BookmarkFilter { WorkflowInstanceId = persisted.Id }))
                .OrderBy(bookmark => bookmark.Id, StringComparer.Ordinal).ToArray()));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(scenario == "resumed" ? 1 : 0, host.Probe.Count("activityResumes"));
            Assert.Null(await host.Execution.ExecuteAsync(host.AdmissionId));
            host.Probe.ResuspendOnResume = false;
            client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(persisted.Id);
            var completed = await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = Assert.Single(persisted.WorkflowState.Bookmarks).Id });
            Assert.Equal(WorkflowSubStatus.Finished, completed.SubStatus);
            Assert.Equal(AdmissionState.Terminal, (await host.Store.FindAsync(host.AdmissionId))!.State);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(scenario == "resumed" ? 2 : 1, host.Probe.Count("activityResumes"));
            Assert.Equal(0, host.Services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount);
            await ObserveAsync(caseId, nameof(ConfirmedSuspendedCheckpointSurvivesObserverFailureAndRemainsResumable), caseId,
                new() { ["observerExceptionPropagated"] = true, ["confirmedCheckpointUnchanged"] = true,
                    ["durableBookmarksUnchanged"] = true, ["resolutionDeniedWhileOwnerHeld"] = true,
                    ["legitimateContinuationCompleted"] = true, ["activityEffects"] = 1,
                    ["activityResumes"] = scenario == "resumed" ? 2 : 1, ["activeCyclesAfterUnwind"] = 0 });
        });
    }

    [Fact]
    public async Task UnknownCheckpointCommitResponseStillRequiresRecoveryDespiteCommittedReadback()
    {
        var fault = new UnknownCheckpointCommit();
        await new AdmissionRuntimeTestFixture(fixture).RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            fault.Armed = true;
            await Assert.ThrowsAsync<IOException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.True(fault.ObservedCommittedCheckpoint);
            Assert.Equal(0, host.Probe.Count("CheckpointRecorded"));
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.RecoveryRequired, record.State);
            Assert.Equal("execution-or-checkpoint-incomplete", record.RecoveryCode);
            Assert.False(record.AuthorityOutstanding);
            Assert.NotNull(record.CheckpointFingerprint);
            var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
            var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
            var persisted = (await instances.FindByIdAsync(record.WorkflowInstanceId!))!;
            Assert.Equal(WorkflowSubStatus.Suspended, persisted.SubStatus);
            var bookmark = Assert.Single(persisted.WorkflowState.Bookmarks);
            var original = serializer.Serialize(persisted.WorkflowState);
            await host.Execution.RecoverAsync(record.Id);
            Assert.Null(await host.Execution.ExecuteAsync(record.Id));
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(persisted.Id);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = bookmark.Id }));
            Assert.Equal(original, serializer.Serialize((await instances.FindByIdAsync(persisted.Id))!.WorkflowState));
            Assert.Equal(record.Revision, (await host.Store.FindAsync(record.Id))!.Revision);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(0, host.Probe.Count("activityResumes"));
            Assert.Equal(0, host.Services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount);
            await ObserveAsync("runtime-checkpoint-commit-unknown", nameof(UnknownCheckpointCommitResponseStillRequiresRecoveryDespiteCommittedReadback), "default",
                new() { ["committedCheckpointObserved"] = true, ["recoveryRequired"] = true, ["continuationDenied"] = true,
                    ["persistedSuspensionUnchanged"] = true, ["checkpointNotifications"] = 0,
                    ["activityEffects"] = 1, ["activityResumes"] = 0, ["activeCyclesAfterUnwind"] = 0 });
        }, fault);
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeCheckpointTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);

    private sealed class UnknownCheckpointCommit : DbTransactionInterceptor
    {
        public bool Armed { get; set; }
        public bool ObservedCommittedCheckpoint { get; private set; }
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
        {
            if (Armed && eventData.Context!.ChangeTracker.Entries<AdmissionRecord>().Any(entry =>
                    entry.Entity.State == AdmissionState.ExecutionObserved && !entry.Entity.AuthorityOutstanding && entry.Entity.CheckpointFingerprint != null))
            {
                Armed = false;
                ObservedCommittedCheckpoint = true;
                throw new IOException("fixture_checkpoint_commit_outcome_unknown");
            }
            return Task.CompletedTask;
        }
    }
}
