using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeRetentionTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Theory]
    [InlineData("runtime-retention-completed", "completed", WorkflowSubStatus.Finished)]
    [InlineData("runtime-retention-handled-fault", "faulted", WorkflowSubStatus.Faulted)]
    public async Task RealTerminalCleanupKeepsOwnedInstanceFencedAndRetainedCapacityCharged(string caseId, string outcome, WorkflowSubStatus expected)
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.Outcome = outcome;
            var result = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            Assert.Equal(expected, result.SubStatus);
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.Terminal, record.State);
            Assert.Equal(AdmissionTerminalDisposition.Completed, record.TerminalDisposition);
            Assert.False(record.AuthorityOutstanding);
            Assert.True(record.ActiveReservationReleased);
            Assert.NotNull(record.TerminalAt);
            var subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
            Assert.Equal(0, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            var policy = subscription.Configuration.Policy;
            var terminalAt = record.TerminalAt.Value;
            Assert.False(await host.Store.CleanupAsync(record.Id, record.Revision, policy.CleanupAuthority, terminalAt + policy.PayloadRetention - TimeSpan.FromTicks(1)));
            Assert.False(await host.Store.CleanupAsync(record.Id, record.Revision, "fixture-wrong-authority", terminalAt + policy.IdentityHorizon));
            Assert.True(await host.Store.CleanupAsync(record.Id, record.Revision, policy.CleanupAuthority, terminalAt + policy.PayloadRetention));
            var payloadCleaned = (await host.Store.FindAsync(record.Id))!;
            Assert.Null(payloadCleaned.Payload);
            Assert.Equal(record.IdentityHash, payloadCleaned.IdentityHash);
            Assert.Equal(record.EventFingerprint, payloadCleaned.EventFingerprint);
            Assert.Equal(record.PayloadFingerprint, payloadCleaned.PayloadFingerprint);
            Assert.Equal(AdmissionOutcome.Duplicate, (await host.Store.AdmitAsync(AdmissionWorkerHost.Event(), terminalAt + policy.PayloadRetention)).Outcome);
            Assert.Equal(AdmissionOutcome.Quarantined, (await host.Store.AdmitAsync(AdmissionWorkerHost.Event() with { Payload = "synthetic-edited-message" }, terminalAt + policy.PayloadRetention)).Outcome);
            Assert.False(await host.Store.CleanupAsync(record.Id, record.Revision, policy.CleanupAuthority, terminalAt + policy.IdentityHorizon));
            Assert.True(await host.Store.CleanupAsync(record.Id, payloadCleaned.Revision, policy.CleanupAuthority, terminalAt + policy.IdentityHorizon));
            var cleaned = (await host.Store.FindByInstanceAsync(result.WorkflowInstanceId))!;
            Assert.Equal(record.Id, cleaned.Id);
            Assert.Null(cleaned.IdentityHash);
            Assert.Null(cleaned.ProviderEventId);
            Assert.Null(cleaned.Payload);
            Assert.Equal(record.AdmittedConfigurationJson, cleaned.AdmittedConfigurationJson);
            Assert.Equal(record.EventFingerprint, cleaned.EventFingerprint);
            Assert.Equal(record.PayloadFingerprint, cleaned.PayloadFingerprint);
            Assert.True(cleaned.ActiveReservationReleased);
            Assert.False(cleaned.RetainedRecordReleased);
            Assert.False(await host.Store.CleanupAsync(cleaned.Id, cleaned.Revision, policy.CleanupAuthority, terminalAt + policy.IdentityHorizon + TimeSpan.FromDays(1)));
            Assert.Null(await host.Execution.ResolveAsync(cleaned.Id, cleaned.Revision, AdmissionTerminalDisposition.Resolved, "fixture-terminal-no-double-release", true, true));
            subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
            Assert.Equal(0, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            Assert.Equal(AdmissionOutcome.Quarantined, (await host.Store.AdmitAsync(AdmissionWorkerHost.Event(), terminalAt + policy.IdentityHorizon)).Outcome);
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(result.WorkflowInstanceId);
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.RunInstanceAsync(new RunWorkflowInstanceRequest()));
            Assert.Equal(1, host.Probe.Count("workflowExecuting"));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(0, host.Probe.Count("activityResumes"));
            await ObserveAsync(caseId, nameof(RealTerminalCleanupKeepsOwnedInstanceFencedAndRetainedCapacityCharged), caseId,
                new() { ["subStatus"] = expected.ToString(), ["qualifiedTerminal"] = true, ["payloadErased"] = true,
                    ["identityErased"] = true, ["immutableDigestsRetained"] = true, ["instanceOwnershipRetained"] = true,
                    ["retainedRecords"] = 1, ["activeReservations"] = 0, ["doubleReleaseDenied"] = true,
                    ["lateForgottenEventQuarantined"] = true, ["additionalWorkflowEntries"] = 0 });
        });
    }

    [Fact]
    public async Task SuspendedWorkflowIsNotExpiryEligibleAndOnlyActualResumeReleasesActiveCapacity()
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var result = (await host.Execution.ExecuteAsync(host.AdmissionId))!;
            Assert.Equal(WorkflowSubStatus.Suspended, result.SubStatus);
            var suspended = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.ExecutionObserved, suspended.State);
            Assert.Null(suspended.TerminalAt);
            Assert.Null(suspended.TerminalDisposition);
            Assert.False(suspended.ActiveReservationReleased);
            Assert.False(suspended.AuthorityOutstanding);
            var subscription = (await host.Store.FindSubscriptionAsync(suspended.SubscriptionId))!;
            Assert.Equal(1, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            Assert.False(await host.Store.CleanupAsync(suspended.Id, suspended.Revision, subscription.Configuration.Policy.CleanupAuthority,
                AdmissionWorkerHost.Now + subscription.Configuration.Policy.IdentityHorizon + TimeSpan.FromDays(1)));
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(suspended.Id, suspended.Revision,
                AdmissionTerminalDisposition.Resolved, "fixture-unknown-effects-not-attested", true, false));
            var instance = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(result.WorkflowInstanceId))!;
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(result.WorkflowInstanceId);
            var resumed = await client.RunInstanceAsync(new RunWorkflowInstanceRequest { BookmarkId = Assert.Single(instance.WorkflowState.Bookmarks).Id });
            Assert.Equal(WorkflowSubStatus.Finished, resumed.SubStatus);
            var terminal = (await host.Store.FindAsync(suspended.Id))!;
            Assert.Equal(AdmissionState.Terminal, terminal.State);
            Assert.Equal(AdmissionTerminalDisposition.Completed, terminal.TerminalDisposition);
            Assert.True(terminal.ActiveReservationReleased);
            Assert.False(await host.Store.CleanupAsync(terminal.Id, suspended.Revision, subscription.Configuration.Policy.CleanupAuthority,
                terminal.TerminalAt!.Value + subscription.Configuration.Policy.IdentityHorizon));
            subscription = (await host.Store.FindSubscriptionAsync(suspended.SubscriptionId))!;
            Assert.Equal(0, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(1, host.Probe.Count("activityResumes"));
            await ObserveAsync("runtime-retention-suspended-resume", nameof(SuspendedWorkflowIsNotExpiryEligibleAndOnlyActualResumeReleasesActiveCapacity), "default",
                new() { ["suspendedCleanupDenied"] = true, ["unknownEffectsResolutionDenied"] = true, ["staleRevisionCleanupDenied"] = true,
                    ["actualResumeCompleted"] = true, ["activeReservations"] = 0, ["retainedRecords"] = 1, ["activityEffects"] = 1, ["activityResumes"] = 1 });
        });
    }

    [Fact]
    public async Task ExplicitAuditedResolutionAfterOwnerUnwindDoesNotReplayPreparedWork()
    {
        await _runtime.RunAsync(async host =>
        {
            host.Probe.Boundary = boundary => boundary == "StartPrepared" ? throw new IOException("fixture_prepared_owner_unwound") : Task.CompletedTask;
            await Assert.ThrowsAsync<IOException>(() => host.Execution.ExecuteAsync(host.AdmissionId));
            var record = (await host.Store.FindAsync(host.AdmissionId))!;
            Assert.Equal(AdmissionState.RecoveryRequired, record.State);
            Assert.False(record.AuthorityOutstanding);
            Assert.Equal(0, host.Probe.Count("AuthorityConsumed"));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            var instance = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(record.WorkflowInstanceId!))!;
            Assert.False(instance.WorkflowState.IsExecuting);
            var subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
            Assert.False(await host.Store.CleanupAsync(record.Id, record.Revision, subscription.Configuration.Policy.CleanupAuthority,
                AdmissionWorkerHost.Now + subscription.Configuration.Policy.IdentityHorizon));
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(record.Id, record.Revision,
                AdmissionTerminalDisposition.Resolved, "fixture-audited-no-effects", false, true));
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Execution.ResolveAsync(record.Id, record.Revision,
                AdmissionTerminalDisposition.Resolved, "fixture-audited-no-effects", true, false));
            // Explicit fixture operator decision: the awaited owner has fully unwound, the
            // only audited activity/notification counters establish no execution or unknown
            // external effects, and this host has no remote provider. Not lease inference.
            var resolved = (await host.Execution.ResolveAsync(record.Id, record.Revision, AdmissionTerminalDisposition.Resolved,
                "fixture-audited-no-effects", true, true))!;
            Assert.Equal(AdmissionState.Terminal, resolved.State);
            Assert.Equal(AdmissionTerminalDisposition.Resolved, resolved.TerminalDisposition);
            Assert.Equal("fixture-audited-no-effects", resolved.AuditReference);
            Assert.True(resolved.ActiveReservationReleased);
            Assert.Null(await host.Execution.ResolveAsync(resolved.Id, resolved.Revision, AdmissionTerminalDisposition.Resolved,
                "fixture-audited-no-effects", true, true));
            Assert.Null(await host.Execution.ExecuteAsync(resolved.Id));
            await host.Execution.RecoverAsync(resolved.Id);
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
            Assert.Equal(0, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            await ObserveAsync("runtime-retention-audited-resolution", nameof(ExplicitAuditedResolutionAfterOwnerUnwindDoesNotReplayPreparedWork), "default",
                new() { ["ambiguousCleanupDenied"] = true, ["bothOperatorAttestationsRequired"] = true,
                    ["ownerUnwound"] = true, ["qualifiedResolved"] = true, ["auditReferencePersisted"] = true,
                    ["activeReservations"] = 0, ["retainedRecords"] = 1, ["workflowExecuting"] = 0, ["activityEffects"] = 0 });
        });
    }

    [Fact]
    public async Task ActualRecoveryPageExportsOnlySafeStateAndNeverEventContent()
    {
        await _runtime.RunAsync(async host =>
        {
            const string payloadMarker = "synthetic-payload-must-never-be-exported";
            const string eventMarker = "synthetic-provider-event-must-never-be-exported";
            var admitted = await host.Execution.AdmitAsync(AdmissionWorkerHost.Event(eventMarker) with { Payload = payloadMarker });
            Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
            var stored = (await host.Store.FindAsync(admitted.AdmissionId!))!;
            Assert.Equal(payloadMarker, stored.Payload);
            Assert.Equal(eventMarker, stored.ProviderEventId);
            var page = await host.Execution.ListRecoveryAsync(10);
            Assert.Contains(page.Items, item => item.AdmissionId == admitted.AdmissionId && item.State == AdmissionState.Admitted && !item.HasInstance && !item.AuthorityOutstanding);
            var exported = JsonSerializer.Serialize(page);
            Assert.DoesNotContain(payloadMarker, exported);
            Assert.DoesNotContain(eventMarker, exported);
            Assert.DoesNotContain(stored.EventFingerprint, exported);
            Assert.DoesNotContain(stored.PayloadFingerprint, exported);
            Assert.DoesNotContain(stored.AdmittedConfigurationJson, exported);
            Assert.DoesNotContain(AdmissionWorkerHost.TenantId, exported);
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-recovery-safe-export", nameof(ActualRecoveryPageExportsOnlySafeStateAndNeverEventContent), "default",
                new() { ["realAdmissionPresentInRecoveryPage"] = true, ["payloadMarkerExcluded"] = true,
                    ["providerEventMarkerExcluded"] = true, ["configurationAndDigestsExcluded"] = true, ["activityEffects"] = 0 });
        });
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeRetentionTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);
}
