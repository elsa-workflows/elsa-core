using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionIdentityReconciliationTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task StableLogicalInstallationReconciliationAndInactivePinEditRetainOriginalEventAllocation()
    {
        // Ledger-only selected-provider configuration proof. This is not a Socket reinstall,
        // transport reconnect or authorization to execute a newly edited pinned workflow.
        var configuration = AdmissionWorkerHost.Configuration();
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, configuration);
        var store = AdmissionTestLedger.Store(services);
        var message = AdmissionWorkerHost.Event();
        var admitted = await store.AdmitAsync(message, AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        var original = (await store.FindAsync(admitted.AdmissionId!))!;
        var subscription = (await store.FindSubscriptionAsync(configuration.Id))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var reconciled = await store.ProvisionAsync(configuration);
        Assert.Equal(withdrawn.Id, reconciled.Id);
        Assert.Equal(withdrawn.Revision, reconciled.Revision);
        Assert.Equal(withdrawn.ActivationEpoch, reconciled.ActivationEpoch);
        Assert.Equal(withdrawn.ConfigurationJson, reconciled.ConfigurationJson);
        Assert.False(reconciled.Active);
        var duplicate = await store.AdmitAsync(message, AdmissionWorkerHost.Now.AddSeconds(1));
        Assert.Equal(AdmissionOutcome.Duplicate, duplicate.Outcome);
        Assert.True(duplicate.AcknowledgementEligible);
        Assert.Equal(original.Id, duplicate.AdmissionId);
        var replacement = configuration with { InstallationId = "different-logical-installation" };
        Assert.Null(await store.ReconfigureAsync(replacement, reconciled.Revision));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ProvisionAsync(replacement));
        var afterReplacement = (await store.FindSubscriptionAsync(configuration.Id))!;
        Assert.Equal(reconciled.ConfigurationJson, afterReplacement.ConfigurationJson);
        Assert.Equal(reconciled.Revision, afterReplacement.Revision);
        Assert.Equal(reconciled.ActivationEpoch, afterReplacement.ActivationEpoch);
        duplicate = await store.AdmitAsync(message, AdmissionWorkerHost.Now.AddSeconds(2));
        Assert.Equal(AdmissionOutcome.Duplicate, duplicate.Outcome);
        Assert.Equal(original.Id, duplicate.AdmissionId);

        var edited = configuration with
        {
            DefinitionVersionId = "definition-version-explicit-edit", DefinitionVersion = 2,
            DefinitionFingerprint = AdmissionHash.Compute("synthetic-explicit-pin-edit")
        };
        var updated = (await store.ReconfigureAsync(edited, afterReplacement.Revision))!;
        Assert.Equal(configuration.Id, updated.Id);
        Assert.Equal(configuration.InstallationId, updated.Configuration.InstallationId);
        Assert.Equal(configuration.ActivationBoundary, updated.Configuration.ActivationBoundary);
        Assert.Equal(edited.DefinitionVersionId, updated.Configuration.DefinitionVersionId);
        Assert.Equal(edited.DefinitionVersion, updated.Configuration.DefinitionVersion);
        Assert.False(updated.Active);
        Assert.False(updated.BootstrapVerified);
        Assert.Equal(original.IdentityHash, AdmissionHash.Identity(updated.Configuration, message.ProviderEventId));
        // Changed binding is quarantined under the SAME event allocation. It cannot route
        // an already admitted event to a new pin, mint authority, or re-open acknowledgement.
        var changedBinding = await store.AdmitAsync(message, AdmissionWorkerHost.Now.AddSeconds(3));
        Assert.Equal(AdmissionOutcome.Quarantined, changedBinding.Outcome);
        Assert.False(changedBinding.AcknowledgementEligible);
        Assert.Equal(original.Id, changedBinding.AdmissionId);
        var retained = (await store.FindAsync(original.Id))!;
        Assert.Equal(original.Revision, retained.Revision);
        Assert.Equal(AdmissionState.Admitted, retained.State);
        Assert.Null(retained.WorkflowInstanceId);
        Assert.False(retained.AuthorityOutstanding);
        Assert.Equal(original.IdentityHash, retained.IdentityHash);
        Assert.Equal(original.EventFingerprint, retained.EventFingerprint);
        Assert.Equal(original.AdmittedConfigurationJson, retained.AdmittedConfigurationJson);
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        Assert.Equal(1, await db.Admissions.CountAsync());
        Assert.Equal(1, updated.ActiveReservations);
        Assert.Equal(1, updated.RetainedRecords);
        await AdmissionProofObservation.WriteAsync(fixture, "provider-installation-reconciliation",
            GetType().FullName + "." + nameof(StableLogicalInstallationReconciliationAndInactivePinEditRetainOriginalEventAllocation), "default", [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true },
            new Dictionary<string, object> { ["sameLogicalInstallationReconciled"] = true, ["matchingDuplicateSameAllocation"] = true,
                ["installationReplacementDenied"] = true, ["inactivePinEditPersisted"] = true, ["editedBindingQuarantinedSameAllocation"] = true,
                ["originalSnapshotAndRevisionRetained"] = true, ["admissionCount"] = 1, ["activeReservations"] = 1,
                ["retainedRecords"] = 1, ["authorityOutstanding"] = false });
    }
}
