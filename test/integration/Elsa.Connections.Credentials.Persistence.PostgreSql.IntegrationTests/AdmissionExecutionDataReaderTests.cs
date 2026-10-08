using System.Data.Common;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionExecutionDataReaderTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("runtime-consumed-data-classic", false)]
    [InlineData("runtime-consumed-data-shell", true)]
    public async Task OnlyExactConsumedOwnerCanReadDurableEvent(string caseId, bool shell)
    {
        await new AdmissionRuntimeTestFixture(fixture).RunAsync(async host =>
        {
            var reader = host.Services.GetRequiredService<IAdmissionExecutionDataReader>();
            var readCount = 0;
            var beforeDenied = false;
            WorkflowExecutionContext? consumedContext = null;
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.BeforeRunnerEntry))
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadConsumedEventAsync(host.Probe.PreparedContext!).AsTask());
                    beforeDenied = true;
                }
            };
            host.Probe.OnExecuting = async context =>
            {
                consumedContext = context;
                var data = await reader.ReadConsumedEventAsync(context);
                var record = (await host.Store.FindAsync(host.AdmissionId))!;
                Assert.Equal(record.Payload, data.Payload);
                Assert.Equal(record.PayloadFingerprint, data.PayloadFingerprint);
                Assert.Equal(record.EventFingerprint, data.EventFingerprint);
                Assert.Equal(record.ProviderEventId, data.ProviderEventId);
                Assert.Equal(record.EventOccurredAt, data.OccurredAt);
                Assert.Equal(record.ActivationEpoch, data.ActivationEpoch);
                Assert.Equal(record.ConfigurationFingerprint, data.Configuration.ConfigurationFingerprint);
                Assert.Equal(record.WorkflowInstanceId, data.WorkflowInstanceId);
                var state = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(context.Id))!.WorkflowState;
                var copy = await WorkflowExecutionContext.CreateAsync(host.Services, context.WorkflowGraph, state);
                Assert.NotSame(context, copy);
                await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadConsumedEventAsync(copy).AsTask());
                readCount++;
            };
            Assert.NotNull(await host.Execution.ExecuteAsync(host.AdmissionId));
            Assert.True(beforeDenied);
            Assert.Equal(1, readCount);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.NotNull(consumedContext);
            await Assert.ThrowsAsync<InvalidOperationException>(() => reader.ReadConsumedEventAsync(consumedContext).AsTask());
            await AdmissionProofObservation.WriteAsync(fixture, caseId,
                $"{typeof(AdmissionExecutionDataReaderTests).FullName}.{nameof(OnlyExactConsumedOwnerCanReadDurableEvent)}", caseId, [],
                new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                new Dictionary<string, object> { ["exactConsumedDataRead"] = true, ["unconsumedCopiedRetiredDenied"] = true, ["activityEffects"] = 1 });
        }, shell: shell);
    }

    [Fact]
    public async Task DataLookupCompletingAfterOwnerRetiresCannotReturnItsOldSnapshot()
    {
        const string caseId = "runtime-consumed-data-retired-read";
        var barrier = new DataReadBarrier();
        await new AdmissionRuntimeTestFixture(fixture).RunAsync(async host =>
        {
            var reader = host.Services.GetRequiredService<IAdmissionExecutionDataReader>();
            Task<AdmissionExecutionData>? delayed = null;
            host.Probe.OnExecuting = async context =>
            {
                barrier.Arm();
                delayed = reader.ReadConsumedEventAsync(context).AsTask();
                await barrier.Reached.Task.WaitAsync(TimeSpan.FromSeconds(30));
            };
            try
            {
                Assert.NotNull(await host.Execution.ExecuteAsync(host.AdmissionId));
                Assert.NotNull(delayed);
                Assert.False(delayed.IsCompleted);
                Assert.Equal(1, host.Probe.Count("activityEffects"));
                Assert.Equal(AdmissionState.Terminal, (await host.Store.FindAsync(host.AdmissionId))!.State);
                barrier.Release.TrySetResult();
                await Assert.ThrowsAsync<InvalidOperationException>(() => delayed!);
                await AdmissionProofObservation.WriteAsync(fixture, caseId,
                    $"{typeof(AdmissionExecutionDataReaderTests).FullName}.{nameof(DataLookupCompletingAfterOwnerRetiresCannotReturnItsOldSnapshot)}", caseId, [],
                    new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                    new Dictionary<string, object> { ["postLookupOwnerRevalidated"] = true, ["staleDataNotReturned"] = true, ["activityEffects"] = 1 });
            }
            finally
            {
                barrier.Release.TrySetResult();
                if (delayed != null)
                {
                    try
                    {
                        await delayed;
                    }
                    catch (InvalidOperationException)
                    {
                        // The assertion above already requires this denial; teardown releases a failed test too.
                    }
                }
            }
        }, barrier);
    }

    private sealed class DataReadBarrier : DbCommandInterceptor
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
                predicate.Contains(".\"Id\" =", StringComparison.Ordinal) && Interlocked.Exchange(ref _armed, 0) == 1)
            {
                // Hold the actual SELECT snapshot while the authorized runner completes and retires.
                Reached.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
}
