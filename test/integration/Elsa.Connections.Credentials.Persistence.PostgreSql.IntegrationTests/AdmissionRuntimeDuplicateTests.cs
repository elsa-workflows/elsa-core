using System.Data.Common;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Options;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeDuplicateTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("runtime-stale-materialized-owner", false)]
    [InlineData("runtime-stale-continuation-owner", true)]
    public async Task DelayedDuplicateCannotReclassifyWinningCheckpointAfterOwnerUnwinds(string caseId, bool continuation)
    {
        var barrier = new SnapshotReadGate(continuation);
        await new AdmissionRuntimeTestFixture(fixture).RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            if (continuation)
            {
                Assert.NotNull(await host.Execution.ExecuteAsync(host.AdmissionId));
            }
            else
            {
                await MaterializeAsync(host);
            }
            var before = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(continuation ? AdmissionState.ExecutionObserved : AdmissionState.Materialized, before.State);
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(before.WorkflowInstanceId!);
            var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
            var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
            var original = (await instances.FindByIdAsync(before.WorkflowInstanceId!))!.WorkflowState;
            var request = new RunWorkflowInstanceRequest { BookmarkId = continuation ? Assert.Single(original.Bookmarks).Id : null };
            barrier.Arm();
            var delayed = continuation ? client.RunInstanceAsync(request) : host.Execution.ExecuteAsync(host.AdmissionId);
            try
            {
                await barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
                host.Probe.ResuspendOnResume = continuation;
                var winner = continuation ? await client.RunInstanceAsync(request) : await host.Execution.ExecuteAsync(host.AdmissionId);
                Assert.Equal(WorkflowSubStatus.Suspended, winner!.SubStatus);
                host.Probe.ResuspendOnResume = false;
                var checkpoint = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(AdmissionState.ExecutionObserved, checkpoint.State);
                Assert.True(checkpoint.Revision > before.Revision);
                Assert.False(checkpoint.AuthorityOutstanding);
                var persisted = serializer.Serialize((await instances.FindByIdAsync(before.WorkflowInstanceId!))!.WorkflowState);
                var executing = host.Probe.Count("workflowExecuting");
                var started = host.Probe.Count("workflowStarted");
                var resumes = host.Probe.Count("activityResumes");
                Assert.Equal(continuation ? 1 : 0, resumes);
                barrier.Release.TrySetResult();
                if (continuation)
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => delayed);
                }
                else
                {
                    Assert.Null(await delayed);
                }
                var after = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(checkpoint.Revision, after.Revision);
                Assert.Equal(checkpoint.State, after.State);
                Assert.Equal(checkpoint.CheckpointFingerprint, after.CheckpointFingerprint);
                Assert.Equal(checkpoint.BookmarkIdsJson, after.BookmarkIdsJson);
                Assert.Null(after.RecoveryCode);
                Assert.Equal(persisted, serializer.Serialize((await instances.FindByIdAsync(before.WorkflowInstanceId!))!.WorkflowState));
                Assert.Equal(executing, host.Probe.Count("workflowExecuting"));
                Assert.Equal(started, host.Probe.Count("workflowStarted"));
                Assert.Equal(resumes, host.Probe.Count("activityResumes"));
                Assert.Equal(1, host.Probe.Count("activityEffects"));
                var state = (await instances.FindByIdAsync(before.WorkflowInstanceId!))!.WorkflowState;
                var completed = await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = Assert.Single(state.Bookmarks).Id });
                Assert.Equal(WorkflowSubStatus.Finished, completed.SubStatus);
                Assert.Equal(AdmissionState.Terminal, (await host.Store.FindAsync(host.AdmissionId))!.State);
                Assert.Equal(continuation ? 2 : 1, host.Probe.Count("activityResumes"));
                await AdmissionProofObservation.WriteAsync(fixture, caseId,
                    $"{typeof(AdmissionRuntimeDuplicateTests).FullName}.{nameof(DelayedDuplicateCannotReclassifyWinningCheckpointAfterOwnerUnwinds)}", caseId, [],
                    new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                    new Dictionary<string, object>
                    {
                        ["staleSnapshotRejected"] = true, ["winningCheckpointUnchanged"] = true,
                        ["legitimateContinuationCompleted"] = true, ["additionalActivityEffects"] = 0,
                        ["additionalResumeEffects"] = 0, ["winnerResumes"] = resumes
                    });
            }
            finally
            {
                barrier.Release.TrySetResult();
                try
                {
                    await delayed;
                }
                catch (InvalidOperationException) when (continuation)
                {
                    // A stale continuation is a definite rejected duplicate.
                }
            }
        }, barrier);
    }

    private static async Task MaterializeAsync(AdmissionRuntimeScenario host)
    {
        var record = (await host.Store.FindAsync(host.AdmissionId))!;
        var binding = host.Services.GetRequiredService<AdmissionRuntimeBinding>();
        var definitions = host.Services.GetRequiredService<IWorkflowDefinitionService>();
        var definition = (await definitions.FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
        var graph = await definitions.MaterializeWorkflowAsync(definition);
        var instanceId = Guid.NewGuid().ToString("N");
        record = (await host.Store.BeginCreationAsync(record.Id, record.Revision, instanceId))!;
        var instances = host.Services.GetRequiredService<IWorkflowInstanceManager>();
        var instance = instances.CreateWorkflowInstance(graph.Workflow, new WorkflowInstanceOptions
        {
            WorkflowInstanceId = instanceId,
            Input = new Dictionary<string, object>
            {
                ["Event"] = record.Payload!, ["ProviderEventId"] = record.ProviderEventId!, ["ChannelId"] = binding.Configuration.ChannelId
            }
        });
        await instances.CreateAsync(instance);
        var persisted = (await instances.FindByIdAsync(instanceId))!;
        var serializer = host.Services.GetRequiredService<IWorkflowStateSerializer>();
        Assert.Equal(serializer.Serialize(instance.WorkflowState), serializer.Serialize(persisted.WorkflowState));
        var fingerprint = AdmissionHash.Compute(serializer.Serialize(persisted.WorkflowState));
        Assert.NotNull(await host.Store.CompleteCreationAsync(record.Id, record.Revision, fingerprint));
    }

    private sealed class SnapshotReadGate(bool continuation) : DbCommandInterceptor
    {
        private int _armed;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public void Arm() => Volatile.Write(ref _armed, 1);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            var predicate = command.CommandText.Split("WHERE", StringSplitOptions.None).Last();
            if (command.CommandText.Contains("\"Admissions\" AS", StringComparison.Ordinal) &&
                predicate.Contains(continuation ? ".\"WorkflowInstanceId\" =" : ".\"Id\" =", StringComparison.Ordinal) &&
                Interlocked.Exchange(ref _armed, 0) == 1)
            {
                // The real PostgreSQL SELECT has executed with its statement snapshot. Hold
                // its old row before EF returns it, while another local owner runs/unwinds.
                Reached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
}
