using System.Text.Json;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionStaleWithdrawalTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task StaleWithdrawalAfterRealAdmissionCannotMutateSubscriptionOrRetractPublishedDefinition()
    {
        await new AdmissionRuntimeTestFixture(fixture).RunUnprovisionedAsync(async host =>
        {
            var services = host.Services;
            var original = await AdmissionRuntimeHost.BootstrapAsync(services);
            Assert.True(original.Active);
            var store = services.GetRequiredService<IAdmissionStore>();
            var execution = services.GetRequiredService<AdmissionExecutionService>();
            var admitted = await execution.AdmitAsync(AdmissionWorkerHost.Event());
            Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
            Assert.True(admitted.AcknowledgementEligible);
            var current = (await store.FindSubscriptionAsync(original.Id))!;
            Assert.Equal(original.Revision + 1, current.Revision);
            Assert.Equal(original.ConfigurationJson, current.ConfigurationJson);
            Assert.True(current.Active);
            Assert.True(current.BootstrapVerified);
            Assert.False(current.Retired);
            Assert.Null(current.ReconciliationCode);
            Assert.Equal(1, current.ActiveReservations);
            Assert.Equal(1, current.RetainedRecords);
            var record = (await store.FindAsync(admitted.AdmissionId!))!;
            Assert.Equal(AdmissionState.Admitted, record.State);
            Assert.False(record.AuthorityOutstanding);
            Assert.Null(record.WorkflowInstanceId);
            var subscriptionSnapshot = JsonSerializer.Serialize(current);
            var recordSnapshot = JsonSerializer.Serialize(record);
            var definitions = services.GetRequiredService<IWorkflowDefinitionService>();
            var serializer = services.GetRequiredService<IPayloadSerializer>();
            var definition = (await definitions.FindWorkflowDefinitionAsync(current.Configuration.DefinitionVersionId))!;
            Assert.True(definition.IsPublished);
            var fingerprint = AdmissionDefinitionFingerprint.Compute(definition, serializer);
            Assert.Equal(current.Configuration.DefinitionFingerprint, fingerprint);

            async Task<(AdmissionSubscription Subscription, AdmissionRecord Record)> AssertUnchangedAsync()
            {
                var subscription = (await store.FindSubscriptionAsync(original.Id))!;
                var admission = (await store.FindAsync(record.Id))!;
                Assert.Equal(subscriptionSnapshot, JsonSerializer.Serialize(subscription));
                Assert.Equal(recordSnapshot, JsonSerializer.Serialize(admission));
                var published = (await definitions.FindWorkflowDefinitionAsync(current.Configuration.DefinitionVersionId))!;
                Assert.True(published.IsPublished);
                Assert.Equal(fingerprint, AdmissionDefinitionFingerprint.Compute(published, serializer));
                Assert.Equal(0, host.Probe.Count("definitionRetracting"));
                Assert.Equal(0, host.Probe.Count("workflowExecuting"));
                Assert.Equal(0, host.Probe.Count("activityEffects"));
                return (subscription, admission);
            }

            // Retirement and reconciliation would visibly mutate the row if a stale CAS
            // incorrectly invoked its update; the real admission made this revision stale.
            var stale = await store.WithdrawAsync(original.Id, original.Revision, true, "fixture-stale-withdrawal");
            Assert.Null(stale);
            await AssertUnchangedAsync();
            var rejected = await Assert.ThrowsAsync<InvalidOperationException>(() => services.GetRequiredService<AdmissionBootstrapService>()
                .WithdrawAndRetractAsync(original.Id, original.Revision, retire: true));
            Assert.Equal("Withdrawal did not establish its authoritative ledger boundary.", rejected.Message);
            var (unchanged, retained) = await AssertUnchangedAsync();
            var finalDefinition = (await definitions.FindWorkflowDefinitionAsync(current.Configuration.DefinitionVersionId))!;
            await AdmissionProofObservation.WriteAsync(fixture, "runtime-stale-withdrawal",
                GetType().FullName + "." + nameof(StaleWithdrawalAfterRealAdmissionCannotMutateSubscriptionOrRetractPublishedDefinition), "default", [],
                new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
                new Dictionary<string, object>
                {
                    ["admissionRevisionAdvanced"] = current.Revision == original.Revision + 1,
                    ["providerConflict"] = stale == null,
                    ["bootstrapConflict"] = rejected.Message == "Withdrawal did not establish its authoritative ledger boundary.",
                    ["subscriptionUnchanged"] = subscriptionSnapshot == JsonSerializer.Serialize(unchanged),
                    ["admittedRecordUnchanged"] = recordSnapshot == JsonSerializer.Serialize(retained),
                    ["definitionPublished"] = finalDefinition.IsPublished,
                    ["definitionFingerprintUnchanged"] = fingerprint == AdmissionDefinitionFingerprint.Compute(finalDefinition, serializer),
                    ["activeReservations"] = unchanged.ActiveReservations, ["retainedRecords"] = unchanged.RetainedRecords,
                    ["retractionNotifications"] = host.Probe.Count("definitionRetracting"),
                    ["workflowExecuting"] = host.Probe.Count("workflowExecuting"), ["activityEffects"] = host.Probe.Count("activityEffects")
                });
        });
    }
}
