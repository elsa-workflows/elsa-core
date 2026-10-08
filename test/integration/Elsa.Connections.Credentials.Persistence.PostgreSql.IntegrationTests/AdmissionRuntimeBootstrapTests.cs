using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionRuntimeBootstrapTests(PostgreSqlConnectionsFixture fixture)
{
    private readonly AdmissionRuntimeTestFixture _runtime = new(fixture);

    [Theory]
    [InlineData("runtime-bootstrap-inactive-classic", false)]
    [InlineData("runtime-bootstrap-inactive-shell", true)]
    public async Task VerifiedInactiveBootstrapRepeatsWithoutPublicationThenExplicitlyActivates(string caseId, bool shell)
    {
        await _runtime.RunUnprovisionedAsync(async host =>
        {
            var services = host.Services;
            var subscription = await AdmissionRuntimeHost.BootstrapAsync(services, activate: false);
            Assert.True(subscription.BootstrapVerified);
            Assert.False(subscription.Active);
            Assert.Equal(0, subscription.ActivationEpoch);
            Assert.Equal(1, host.Probe.Count("definitionPublished"));
            var execution = services.GetRequiredService<AdmissionExecutionService>();
            Assert.Equal(AdmissionOutcome.Inactive, (await execution.AdmitAsync(AdmissionWorkerHost.Event())).Outcome);
            var repeated = await AdmissionRuntimeHost.BootstrapAsync(services, activate: false);
            Assert.Equal(subscription.Revision, repeated.Revision);
            Assert.True(repeated.BootstrapVerified);
            Assert.False(repeated.Active);
            Assert.Equal(1, host.Probe.Count("definitionPublished"));
            var binding = services.GetRequiredService<AdmissionRuntimeBinding>();
            var definition = (await services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
            Assert.True(definition.IsPublished);
            Assert.Equal(binding.Configuration.DefinitionFingerprint, AdmissionDefinitionFingerprint.Compute(definition, services.GetRequiredService<IPayloadSerializer>()));
            var activated = (await services.GetRequiredService<AdmissionBootstrapService>().ActivateAsync(repeated.Id, repeated.Revision))!;
            Assert.True(activated.Active);
            Assert.True(activated.ActivationEpoch > repeated.ActivationEpoch);
            var admitted = await execution.AdmitAsync(AdmissionWorkerHost.Event());
            Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
            Assert.NotNull(await execution.ExecuteAsync(admitted.AdmissionId!));
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(1, host.Probe.Count("definitionPublished"));
            await ObserveAsync(caseId, nameof(VerifiedInactiveBootstrapRepeatsWithoutPublicationThenExplicitlyActivates), caseId,
                new() { ["inactiveBeforeExplicitActivation"] = true, ["fullDefinitionReloadVerified"] = true,
                    ["publicationNotifications"] = 1, ["activationEpochAdvanced"] = true, ["activityEffects"] = 1 });
        }, shell: shell);
    }

    [Theory]
    [InlineData("runtime-bootstrap-autonomous-start", "autonomous")]
    [InlineData("runtime-bootstrap-unallowlisted-activity", "unallowlisted")]
    public async Task ForbiddenGraphIsRejectedAfterRealMaterializationBeforePersistence(string caseId, string scenario)
    {
        await _runtime.RunUnprovisionedAsync(async host =>
        {
            var services = host.Services;
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync();
            var binding = services.GetRequiredService<AdmissionRuntimeBinding>();
            Assert.Equal(binding.Configuration.DefinitionFingerprint, AdmissionDefinitionFingerprint.Compute(binding.Artifact, services.GetRequiredService<IPayloadSerializer>()));
            var graph = await services.GetRequiredService<IWorkflowDefinitionService>().MaterializeWorkflowAsync(binding.Artifact);
            if (scenario == "autonomous")
            {
                Assert.Contains(graph.Nodes, node => node.Activity is AdmissionRuntimeActivity && node.Activity.GetCanStartWorkflow());
            }
            else
            {
                Assert.Contains(graph.Nodes, node => node.Activity is WriteLine);
                Assert.DoesNotContain(graph.Nodes, node => node.Activity.GetCanStartWorkflow());
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => AdmissionRuntimeHost.BootstrapAsync(services, activate: false));
            Assert.Null(await services.GetRequiredService<IAdmissionStore>().FindSubscriptionAsync(binding.Configuration.Id));
            Assert.Null(await services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id));
            Assert.Equal(0, host.Probe.Count("definitionPublished"));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync(caseId, nameof(ForbiddenGraphIsRejectedAfterRealMaterializationBeforePersistence), caseId,
                new() { ["allowlistedFingerprintMatched"] = true, ["realForbiddenGraphMaterialized"] = true,
                    ["subscriptionAbsent"] = true, ["definitionAbsent"] = true, ["publicationNotifications"] = 0,
                    ["workflowExecuting"] = 0, ["activityEffects"] = 0 });
        }, canStartWorkflow: scenario == "autonomous", unallowlistedActivity: scenario == "unallowlisted");
    }

    [Fact]
    public async Task MismatchedPredefinedArtifactCannotCreateDefinitionOrSubscription()
    {
        await _runtime.RunUnprovisionedAsync(async host =>
        {
            var binding = host.Services.GetRequiredService<AdmissionRuntimeBinding>();
            var artifact = binding.Artifact.ShallowClone();
            artifact.Description = "synthetic-unapproved-content";
            await Assert.ThrowsAsync<InvalidOperationException>(() => host.Services.GetRequiredService<AdmissionBootstrapService>()
                .ProvisionAsync(binding.Configuration, artifact));
            Assert.Null(await host.Services.GetRequiredService<IAdmissionStore>().FindSubscriptionAsync(binding.Configuration.Id));
            Assert.Null(await host.Services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id));
            Assert.Equal(0, host.Probe.Count("definitionPublished"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-bootstrap-artifact-mismatch", nameof(MismatchedPredefinedArtifactCannotCreateDefinitionOrSubscription), "default",
                new() { ["subscriptionAbsent"] = true, ["definitionAbsent"] = true, ["publicationNotifications"] = 0, ["activityEffects"] = 0 });
        });
    }

    [Fact]
    public async Task ExistingUnverifiedInsertNeverReplaysPublicationOrBecomesActive()
    {
        await _runtime.RunUnprovisionedAsync(async host =>
        {
            var services = host.Services;
            var binding = services.GetRequiredService<AdmissionRuntimeBinding>();
            var store = services.GetRequiredService<IAdmissionStore>();
            await store.ProvisionAsync(binding.Configuration);
            var definitions = services.GetRequiredService<IAdmissionDefinitionBootstrapStore>();
            // Actual definite insert with no publication emulates the durable boundary left by
            // a coordinator that died before publication; readback cannot authorize its replay.
            await using (await definitions.AcquireExclusiveAsync(binding.Configuration))
            {
                Assert.Equal(AdmissionDefinitionInsertOutcome.Inserted,
                    await definitions.InsertOrVerifyAsync(binding.Artifact, binding.Configuration.DefinitionFingerprint));
            }
            await Assert.ThrowsAsync<InvalidOperationException>(() => AdmissionRuntimeHost.BootstrapAsync(services));
            var subscription = (await store.FindSubscriptionAsync(binding.Configuration.Id))!;
            Assert.False(subscription.Active);
            Assert.False(subscription.BootstrapVerified);
            Assert.Equal("bootstrap-outcome-unknown", subscription.ReconciliationCode);
            var definition = (await services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
            Assert.False(definition.IsPublished);
            Assert.Equal(binding.Configuration.DefinitionFingerprint, AdmissionDefinitionFingerprint.Compute(definition, services.GetRequiredService<IPayloadSerializer>()));
            await Assert.ThrowsAsync<InvalidOperationException>(() => AdmissionRuntimeHost.BootstrapAsync(services));
            await Assert.ThrowsAsync<InvalidOperationException>(() => services.GetRequiredService<AdmissionBootstrapService>().ActivateAsync(subscription.Id, subscription.Revision));
            await Assert.ThrowsAsync<InvalidOperationException>(() => services.GetRequiredService<AdmissionExecutionService>().AdmitAsync(AdmissionWorkerHost.Event()));
            Assert.Equal(0, host.Probe.Count("definitionPublished"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-bootstrap-unverified-insert", nameof(ExistingUnverifiedInsertNeverReplaysPublicationOrBecomesActive), "default",
                new() { ["fullDefinitionReloadVerified"] = true, ["subscriptionInactive"] = true, ["bootstrapUnverified"] = true,
                    ["reconciliationRequired"] = true, ["publicationNotifications"] = 0, ["activityEffects"] = 0 });
        });
    }

    [Fact]
    public async Task VerifiedBootstrapRejectsChangedStoredContentWithoutOverwriteOrRepublish()
    {
        await _runtime.RunUnprovisionedAsync(async host =>
        {
            var services = host.Services;
            await AdmissionRuntimeHost.BootstrapAsync(services, activate: false);
            var binding = services.GetRequiredService<AdmissionRuntimeBinding>();
            var definitions = services.GetRequiredService<IWorkflowDefinitionStore>();
            var definition = (await services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
            // Deliberate trusted-fixture corruption through the actual normal store. This is
            // not an exposed supported management route or an alternate execution host.
            definition.Description = "synthetic-stored-content-change";
            await definitions.SaveAsync(definition);
            await Assert.ThrowsAsync<InvalidOperationException>(() => AdmissionRuntimeHost.BootstrapAsync(services, activate: false));
            var reloaded = (await services.GetRequiredService<IWorkflowDefinitionService>().FindWorkflowDefinitionAsync(binding.Artifact.Id))!;
            Assert.Equal(definition.Description, reloaded.Description);
            Assert.NotEqual(binding.Configuration.DefinitionFingerprint, AdmissionDefinitionFingerprint.Compute(reloaded, services.GetRequiredService<IPayloadSerializer>()));
            var subscription = (await services.GetRequiredService<IAdmissionStore>().FindSubscriptionAsync(binding.Configuration.Id))!;
            Assert.False(subscription.Active);
            Assert.NotNull(subscription.ReconciliationCode);
            Assert.Equal(1, host.Probe.Count("definitionPublished"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            await ObserveAsync("runtime-bootstrap-stored-content-change", nameof(VerifiedBootstrapRejectsChangedStoredContentWithoutOverwriteOrRepublish), "default",
                new() { ["changedStoredContentPreserved"] = true, ["subscriptionInactive"] = true,
                    ["reconciliationRequired"] = true, ["publicationNotifications"] = 1, ["activityEffects"] = 0 });
        });
    }

    private Task ObserveAsync(string caseId, string method, string parameterId, Dictionary<string, object> facts) =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(AdmissionRuntimeBootstrapTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["behaviorAssertionsPassed"] = true }, facts);
}
