using System.Text.Json;
using Elsa.Common.Models;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AdmissionPackageConsumer;

internal static class Scenarios
{
    public static async Task<ScenarioEvidence> AdmissionAsync(ConsumerOptions options, CancellationToken cancellationToken)
    {
        var database = await Migrations.DatabaseAsync(options.AdmissionConnection, cancellationToken);
        var lifetimes = new HostLifetimes();
        MigrationEvidence[] migrations;
        Dictionary<string, int> counters;
        await using (var host = ConsumerHost.Create(options.AdmissionConnection, options.Feature, true, lifetimes))
        {
            using var tenant = host.EnterTenant();
            await using var scope = host.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await Migrations.ApplyAsync(services, true, cancellationToken);
            await host.Probe.InitializeAsync(cancellationToken);
            await host.StartAsync(cancellationToken);
            await services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services, cancellationToken);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync(cancellationToken);
            Require.That(services.GetRequiredService<IWorkflowRuntime>().GetType() == typeof(AdmissionWorkflowRuntime), "admission-runtime-not-selected");
            Require.That(services.GetRequiredService<IWorkflowRunner>().GetType() == typeof(WorkflowRunner), "built-in-runner-not-selected");
            Require.That(services.GetService<EndpointDataSource>()?.Endpoints.Count is null or 0, "unexpected-endpoints");
            var binding = services.GetRequiredService<AdmissionBinding>();
            var bootstrap = services.GetRequiredService<AdmissionBootstrapService>();
            var subscription = await bootstrap.ProvisionAsync(binding.Configuration, binding.Artifact, cancellationToken);
            Require.That(!subscription.Active && subscription.BootstrapVerified && !subscription.Retired, "inactive-verified-bootstrap");
            var store = services.GetRequiredService<IAdmissionStore>();
            var persistenceScope = services.GetRequiredService<AdmissionPersistenceScope>();
            Require.That(store.GetType() == typeof(EFCoreAdmissionStore) && persistenceScope.TenantId == FixtureConstants.TenantId &&
                persistenceScope.EnvironmentId == FixtureConstants.EnvironmentId, "admission-store-scope");
            VerifyWorkflowStores(services);
            var inactive = await store.FindSubscriptionAsync(subscription.Id, cancellationToken);
            Require.That(inactive is { Active: false, BootstrapVerified: true }, "bootstrap-not-durable");
            subscription = await bootstrap.ActivateAsync(subscription.Id, subscription.Revision, cancellationToken)
                ?? throw new ProofFailure("activation-failed");
            Require.That(subscription.Active, "subscription-not-active");
            var activated = await store.FindSubscriptionAsync(subscription.Id, cancellationToken);
            Require.That(activated is { Active: true, BootstrapVerified: true } && activated.Revision == subscription.Revision &&
                activated.ActivationEpoch == subscription.ActivationEpoch, "activation-not-persisted");
            var execution = services.GetRequiredService<AdmissionExecutionService>();
            var admitted = await execution.AdmitAsync(new(binding.Configuration.Id, binding.Configuration.InstallationId, binding.Configuration.ChannelId,
                "package-event", DateTimeOffset.UtcNow, true, false, "synthetic-human-event"), cancellationToken);
            Require.That(admitted.Outcome == AdmissionOutcome.Committed && admitted.AcknowledgementEligible, "admission-not-committed");
            var suspended = await execution.ExecuteAsync(admitted.AdmissionId!, cancellationToken)
                ?? throw new ProofFailure("execution-not-returned");
            var instances = services.GetRequiredService<IWorkflowInstanceManager>();
            var instance = await instances.FindByIdAsync(suspended.WorkflowInstanceId, cancellationToken)
                ?? throw new ProofFailure("suspended-instance-missing");
            Require.That(instance.TenantId == FixtureConstants.TenantId && instance.SubStatus == WorkflowSubStatus.Suspended &&
                instance.WorkflowState.Bookmarks.Count == 1 && instance.WorkflowState.Incidents.Count == 0, "suspension-not-persisted");
            var bookmark = await SingleBookmarkAsync(services, instance.Id, cancellationToken);
            Require.That(bookmark == instance.WorkflowState.Bookmarks.Single().Id, "bookmark-not-persisted");
            var checkpoint = (await store.FindAsync(admitted.AdmissionId!, cancellationToken))!;
            Require.That(checkpoint.State == AdmissionState.ExecutionObserved && !checkpoint.AuthorityOutstanding &&
                checkpoint.WorkflowInstanceId == instance.Id && checkpoint.CheckpointFingerprint is { Length: 64 }, "checkpoint-not-recorded");
            var before = services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState);
            var executingBefore = host.Probe.ExecutingNotifications;
            var startedBefore = host.Probe.StartedNotifications;
            var context = host.Probe.LastContext ?? throw new ProofFailure("real-context-not-observed");
            var pipeline = services.GetRequiredService<IWorkflowExecutionPipeline>();
            var runner = services.GetRequiredService<IWorkflowRunner>();
            var activityPipeline = services.GetRequiredService<IActivityExecutionPipeline>();
            var activityInvoker = services.GetRequiredService<IActivityInvoker>();
            var activityContext = context.ActivityExecutionContexts.Single(x => x.Activity is AdmissionActivity);
            var client = await services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(instance.Id, cancellationToken);
            var deniedEntries = await DeniedCountAsync(
            [
                () => pipeline.ExecuteAsync(context), () => pipeline.Pipeline(context), () => runner.RunAsync(context),
                () => activityPipeline.ExecuteAsync(activityContext), () => activityPipeline.Pipeline(activityContext),
                () => activityInvoker.InvokeAsync(activityContext), () => activityInvoker.InvokeAsync(context, activityContext.Activity),
                () => client.RunInstanceAsync(new(), cancellationToken), () => client.CancelAsync(cancellationToken),
                () => client.ImportStateAsync(instance.WorkflowState, cancellationToken), () => client.DeleteAsync(cancellationToken)
            ]);
            var managementDenials = await DeniedCountAsync(
            [
                () => services.GetRequiredService<IWorkflowDefinitionManager>().NewAsync(cancellationToken: cancellationToken),
                () => services.GetRequiredService<IWorkflowDefinitionPublisher>().RetractAsync(binding.Artifact.DefinitionId, cancellationToken)
            ]);
            var backgroundDenials = await DeniedCountAsync(
            [
                () => services.GetRequiredService<IConnectionBackgroundUseService>()
                    .ResolveForUseAsync(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "package-connection", cancellationToken)
            ]);
            var lifecycleDenials = await DeniedCountAsync(
            [
                () => services.GetRequiredService<IConnectionLifecycleService>()
                    .DisconnectAsync(FixturePolicies.Principal(), FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "package-connection", cancellationToken),
                () => services.GetRequiredService<IStaticApiKeyLifecycleService>().ConnectApiKeyAsync(FixturePolicies.Principal(),
                    new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "fixture-provider", "fixture-account", FixtureConstants.SecretMarker), cancellationToken)
            ]);
            Require.That(host.Probe.ExecutingNotifications == executingBefore && host.Probe.StartedNotifications == startedBefore, "denied-entry-notification");
            instance = (await instances.FindByIdAsync(instance.Id, cancellationToken))!;
            Require.That(services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState) == before, "denied-entry-mutated-state");
            Require.That((await store.FindAsync(admitted.AdmissionId!, cancellationToken))!.Revision == checkpoint.Revision, "denied-entry-mutated-ledger");
            await CheckNoGrantAsync(services, context, cancellationToken);
            // This ordinary secret exercises selected Secrets migrations in the guarded host;
            // it grants no connection authority and contains no credential marker.
            await services.GetRequiredService<ISecretManager>().CreateAsync(new CreateSecretRequest
            {
                Name = "package-migration-probe", Value = "synthetic-public-migration-probe"
            }, cancellationToken);
            migrations = await Migrations.ReapplyPopulatedAsync(services, true, cancellationToken);
            var resumed = await client.RunInstanceAsync(new() { BookmarkId = bookmark }, cancellationToken);
            instance = (await instances.FindByIdAsync(instance.Id, cancellationToken))!;
            Require.That(resumed.Status == WorkflowStatus.Finished && instance.Status == WorkflowStatus.Finished &&
                instance.SubStatus == WorkflowSubStatus.Finished && instance.WorkflowState.Bookmarks.Count == 0 &&
                instance.WorkflowState.Incidents.Count == 0, "legitimate-resume-not-completed");
            Require.That(!(await services.GetRequiredService<IBookmarkStore>().FindManyAsync(new() { WorkflowInstanceId = instance.Id }, cancellationToken)).Any(), "bookmark-not-consumed");
            var terminal = (await store.FindAsync(admitted.AdmissionId!, cancellationToken))!;
            Require.That(terminal.State == AdmissionState.Terminal && terminal.TerminalDisposition == AdmissionTerminalDisposition.Completed &&
                !terminal.AuthorityOutstanding && terminal.ActiveReservationReleased, "completion-not-terminal");
            Require.That(services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount == 0, "ownership-not-unwound");
            Require.NoSecret(services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState));
            Require.That(await host.Probe.CountAsync("admissionExecute", cancellationToken) == 1 &&
                await host.Probe.CountAsync("admissionResume", cancellationToken) == 1, "activity-effects-not-exact");
            counters = new()
            {
                ["activityExecutions"] = await host.Probe.CountAsync("admissionExecute", cancellationToken),
                ["activityResumes"] = await host.Probe.CountAsync("admissionResume", cancellationToken),
                ["ownedEntryDenials"] = deniedEntries, ["managementDenials"] = managementDenials, ["backgroundDenials"] = backgroundDenials,
                ["lifecycleDenials"] = lifecycleDenials, ["providerCalls"] = host.Probe.Provider.Calls,
                ["activeExecutionCycles"] = services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount
            };
        }
        return new("admission", database.Identity, database.Version, new()
        {
            ["inactiveBootstrapVerified"] = true, ["activationPersisted"] = true, ["admissionCommitted"] = true,
            ["realGuardedRuntimeSelected"] = true, ["realBuiltInRunnerSelected"] = true, ["suspensionPersisted"] = true,
            ["bookmarkPersisted"] = true, ["checkpointRecorded"] = true, ["ownedEntriesDeniedWithoutEffects"] = true,
            ["genericManagementDenied"] = true, ["publicBackgroundDenied"] = true, ["publicLifecycleDenied"] = true,
            ["noImplicitGrant"] = true, ["storedGrantPolicyDenied"] = true, ["resolverDeniedBeforeBackground"] = true,
            ["legitimateResumeCompleted"] = true, ["terminalRecorded"] = true, ["ownershipUnwound"] = true,
            ["noMappedEndpoints"] = true, ["noSecretMarkers"] = true, ["providerNotCalled"] = true,
            ["migrationsReappliedPopulated"] = true
        }, counters, migrations, lifetimes.Evidence(1));
    }

    private static async Task<int> DeniedCountAsync(IEnumerable<Func<Task>> actions)
    {
        var count = 0;
        foreach (var action in actions)
        {
            await Require.DeniedAsync(action);
            count++;
        }
        return count;
    }

    private static async Task CheckNoGrantAsync(IServiceProvider services, WorkflowExecutionContext context, CancellationToken cancellationToken)
    {
        var lifecycle = services.GetRequiredService<IConnectionLifecycleStore>();
        Require.That(lifecycle.GetType() == typeof(EFCoreConnectionLifecycleStore), "real-connection-store-not-selected");
        await lifecycle.CreateAsync(new IntegrationConnection
        {
            Id = "package-connection", TenantId = FixtureConstants.TenantId, EnvironmentId = FixtureConstants.EnvironmentId,
            ProviderId = "fixture-provider", ProviderAccountId = "fixture-account", Status = ConnectionStatus.Active,
            Revision = 1, OperationStatus = CredentialOperationStatus.None
        }, cancellationToken);
        var binding = await services.GetRequiredService<IConnectionCredentialBindingStore>().TryCreateAsync(FixtureConstants.TenantId,
            FixtureConstants.EnvironmentId, FixtureConstants.BindingId, "package-connection", cancellationToken)
            ?? throw new ProofFailure("real-binding-not-created");
        var grants = services.GetRequiredService<IConnectionCredentialUseGrantStore>();
        Require.That(grants.GetType() == typeof(EFCoreConnectionCredentialUseGrantStore), "real-grant-store-not-selected");
        Require.That(await grants.FindAsync(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, context.Id, binding.LogicalBindingId, cancellationToken) == null, "implicit-grant");
        var request = new ConnectionCredentialBindingUseRequest(FixtureConstants.TenantId, FixtureConstants.EnvironmentId,
            binding.LogicalBindingId, binding.ConnectionId, binding.Revision, context.Id);
        Require.That(await services.GetRequiredService<IConnectionCredentialBindingUseAuthorizer>().AuthorizeAsync(request, cancellationToken), "additional-policy-not-allowing");
        Require.That(!await services.GetRequiredService<StoredConnectionCredentialBindingUseAuthorizer>().AuthorizeAsync(request, cancellationToken), "missing-grant-not-denied");
        try
        {
            await services.GetRequiredService<IWorkflowCredentialResolver>().ResolveAsync(context, FixtureConstants.BindingId, cancellationToken);
        }
        catch (ConnectionUnavailableException)
        {
            return;
        }
        throw new ProofFailure("missing-grant-resolver-not-denied");
    }

    public static async Task<ScenarioEvidence> GrantAsync(ConsumerOptions options, CancellationToken cancellationToken)
    {
        var database = await Migrations.DatabaseAsync(options.GrantConnection, cancellationToken);
        var lifetimes = new HostLifetimes();
        MigrationEvidence[] migrations;
        string grantedId;
        string ungrantedId;
        string grantedBookmark;
        string ungrantedBookmark;
        string connectionId;
        long bindingRevision;
        await using (var first = ConsumerHost.Create(options.GrantConnection, options.Feature, false, lifetimes))
        {
            using var tenant = first.EnterTenant();
            await using var scope = first.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await Migrations.ApplyAsync(services, false, cancellationToken);
            await first.Probe.InitializeAsync(cancellationToken);
            await first.StartAsync(cancellationToken);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync(cancellationToken);
            VerifyNonAdmission(services);
            var connected = await services.GetRequiredService<IStaticApiKeyLifecycleService>().ConnectApiKeyAsync(FixturePolicies.Principal(),
                new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "fixture-provider", "fixture-account", FixtureConstants.SecretMarker), cancellationToken);
            Require.That(connected.Succeeded && connected.ConnectionId != null, "lifecycle-connect-failed");
            connectionId = connected.ConnectionId!;
            var policies = services.GetRequiredService<IConnectionUseAuthorizer>() as FixturePolicies
                ?? throw new ProofFailure("fixture-policies-not-selected");
            policies.ConnectionId = connectionId;
            var connection = await services.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId,
                FixtureConstants.TenantId, FixtureConstants.EnvironmentId, cancellationToken);
            Require.That(connection is { Status: ConnectionStatus.Active, CurrentGenerationId: not null, CurrentSecretName: not null }, "managed-generation-missing");
            await VerifyEncryptedSecretAsync(services, connection!, cancellationToken);
            var created = await services.GetRequiredService<IWorkflowCredentialBindingManager>().CreateAsync(FixturePolicies.Principal(),
                FixtureConstants.BindingId, connectionId, cancellationToken);
            Require.That(created.Succeeded && created.Revision.HasValue, "binding-create-failed");
            bindingRevision = created.Revision!.Value;
            var binding = await services.GetRequiredService<IConnectionCredentialBindingStore>().FindAsync(FixtureConstants.TenantId,
                FixtureConstants.EnvironmentId, FixtureConstants.BindingId, cancellationToken);
            Require.That(binding?.ConnectionId == connectionId && binding.Revision == bindingRevision, "binding-readback-failed");
            var runtime = services.GetRequiredService<IWorkflowRuntime>();
            var granted = await StartGrantWorkflowAsync(runtime, cancellationToken);
            var ungranted = await StartGrantWorkflowAsync(runtime, cancellationToken);
            grantedId = granted.WorkflowInstanceId;
            ungrantedId = ungranted.WorkflowInstanceId;
            Require.That(grantedId != ungrantedId, "workflow-identities-not-distinct");
            grantedBookmark = await SingleBookmarkAsync(services, grantedId, cancellationToken);
            ungrantedBookmark = await SingleBookmarkAsync(services, ungrantedId, cancellationToken);
            await AssertSuspendedAsync(services, grantedId, cancellationToken);
            await AssertSuspendedAsync(services, ungrantedId, cancellationToken);
            Require.That(await first.Probe.CountAsync("grantAttempt", cancellationToken) == 0, "credential-resolved-before-grant");
            policies.GrantedInstanceId = grantedId;
            policies.BindingRevision = bindingRevision;
            var issued = await services.GetRequiredService<IWorkflowCredentialGrantManager>().IssueAsync(FixturePolicies.Principal(),
                grantedId, FixtureConstants.BindingId, bindingRevision, cancellationToken);
            Require.That(issued.Succeeded, "grant-issue-failed");
            await VerifyGrantsAsync(services, grantedId, ungrantedId, connectionId, bindingRevision, cancellationToken);
            migrations = await Migrations.ReapplyPopulatedAsync(services, false, cancellationToken);
        }
        // The first host and every scope have completely disposed before this executor exists.
        Dictionary<string, int> counters;
        await using (var second = ConsumerHost.Create(options.GrantConnection, options.Feature, false, lifetimes))
        {
            using var tenant = second.EnterTenant();
            await using var scope = second.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await second.StartAsync(cancellationToken);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync(cancellationToken);
            VerifyNonAdmission(services);
            ((FixturePolicies)services.GetRequiredService<IConnectionUseAuthorizer>()).ConnectionId = connectionId;
            await VerifyGrantsAsync(services, grantedId, ungrantedId, connectionId, bindingRevision, cancellationToken);
            var runtime = services.GetRequiredService<IWorkflowRuntime>();
            // Ungranted first: any accidental authority broadening is visible before the success case.
            foreach (var pair in new[] { (ungrantedId, ungrantedBookmark), (grantedId, grantedBookmark) })
            {
                var client = await runtime.CreateClientAsync(pair.Item1, cancellationToken);
                var result = await client.RunInstanceAsync(new() { BookmarkId = pair.Item2 }, cancellationToken);
                Require.That(result.Status == WorkflowStatus.Finished && result.SubStatus == WorkflowSubStatus.Finished && result.Incidents.Count == 0, "grant-workflow-resume-failed");
                var persisted = await services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(pair.Item1, cancellationToken);
                Require.That(persisted is { Status: WorkflowStatus.Finished } && persisted.WorkflowState.Bookmarks.Count == 0 &&
                    persisted.WorkflowState.Incidents.Count == 0, "grant-finished-state-not-persisted");
                Require.NoSecret(services.GetRequiredService<IWorkflowStateSerializer>().Serialize(persisted!.WorkflowState));
            }
            Require.That(await second.Probe.CountAsync("grantSuspend", cancellationToken) == 2 &&
                await second.Probe.CountAsync("grantAttempt", cancellationToken) == 2 &&
                await second.Probe.CountAsync("grantSuccess", cancellationToken) == 1 &&
                await second.Probe.CountAsync("grantDenied", cancellationToken) == 1, "grant-effects-not-exact");
            Require.That(await second.Probe.CountAsync("grantSuccess:" + grantedId, cancellationToken) == 1 &&
                await second.Probe.CountAsync("grantDenied:" + grantedId, cancellationToken) == 0 &&
                await second.Probe.CountAsync("grantSuccess:" + ungrantedId, cancellationToken) == 0 &&
                await second.Probe.CountAsync("grantDenied:" + ungrantedId, cancellationToken) == 1, "grant-effects-not-instance-bound");
            counters = new()
            {
                ["workflowSuspensions"] = await second.Probe.CountAsync("grantSuspend", cancellationToken),
                ["credentialAttempts"] = await second.Probe.CountAsync("grantAttempt", cancellationToken),
                ["credentialSuccesses"] = await second.Probe.CountAsync("grantSuccess", cancellationToken),
                ["credentialDenials"] = await second.Probe.CountAsync("grantDenied", cancellationToken),
                ["providerCalls"] = second.Probe.Provider.Calls
            };
        }
        return new("managed-secret-grant", database.Identity, database.Version, new()
        {
            ["nonAdmissionHostVerified"] = true, ["realLifecycleUsed"] = true, ["managedSecretEncrypted"] = true,
            ["bindingPersisted"] = true, ["twoRealWorkflowsSuspended"] = true, ["noResolutionBeforeGrant"] = true,
            ["exactInstanceGrantPersisted"] = true, ["ungrantedInstancePolicyDenied"] = true,
            ["sequentialRestartCompleted"] = true, ["grantSurvivedRestart"] = true, ["grantedWorkflowCompleted"] = true,
            ["ungrantedWorkflowCompletedDenied"] = true, ["oneCredentialResolution"] = true,
            ["realResolverAndStoresSelected"] = true, ["noSecretMarkers"] = true, ["providerNotCalled"] = true,
            ["migrationsReappliedPopulated"] = true
        }, counters, migrations, lifetimes.Evidence(2));
    }

    private static void VerifyNonAdmission(IServiceProvider services)
    {
        VerifyWorkflowStores(services);
        Require.That(services.GetService<AdmissionHostConfiguration>() == null && services.GetService<IAdmissionStore>() == null &&
            services.GetService<IDbContextFactory<AdmissionElsaDbContext>>() == null && services.GetService<AdmissionExecutionService>() == null, "admission-registration-in-positive-host");
        Require.That(services.GetRequiredService<IWorkflowRuntime>().GetType() == typeof(LocalWorkflowRuntime) &&
            services.GetRequiredService<IWorkflowCredentialResolver>().GetType() == typeof(WorkflowCredentialResolver) &&
            services.GetRequiredService<IConnectionLifecycleStore>().GetType() == typeof(EFCoreConnectionLifecycleStore) &&
            services.GetRequiredService<IConnectionCredentialBindingStore>().GetType() == typeof(EFCoreConnectionCredentialBindingStore) &&
            services.GetRequiredService<IConnectionCredentialUseGrantStore>().GetType() == typeof(EFCoreConnectionCredentialUseGrantStore), "positive-host-selected-services");
        Require.That(services.GetService<EndpointDataSource>()?.Endpoints.Count is null or 0, "positive-host-endpoints");
    }

    private static void VerifyWorkflowStores(IServiceProvider services)
    {
        Require.That(services.GetRequiredService<IWorkflowDefinitionStore>().GetType() == typeof(EFCoreWorkflowDefinitionStore) &&
            services.GetRequiredService<IWorkflowInstanceStore>().GetType() == typeof(EFCoreWorkflowInstanceStore) &&
            services.GetRequiredService<IBookmarkStore>().GetType() == typeof(EFCoreBookmarkStore), "real-workflow-stores-not-selected");
    }

    private static async Task VerifyEncryptedSecretAsync(IServiceProvider services, IntegrationConnection connection, CancellationToken cancellationToken)
    {
        await using var secrets = await services.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync(cancellationToken);
        var secret = await secrets.Secrets.SingleAsync(x => x.Name == connection.CurrentSecretName, cancellationToken);
        Require.That(secret.ManagedOwnerId == connection.Id && secret.ManagedGenerationId == connection.CurrentGenerationId, "secret-generation-not-bound");
        var encoded = secrets.Entry(secret).Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue;
        Require.That(!string.IsNullOrWhiteSpace(encoded), "encrypted-secret-empty");
        Require.NoSecret(encoded!);
        Require.That(encoded!.Contains("protectedValue", StringComparison.Ordinal), "encrypted-secret-not-protected");
    }

    private static async Task VerifyGrantsAsync(IServiceProvider services, string granted, string ungranted, string connection, long revision, CancellationToken cancellationToken)
    {
        var store = services.GetRequiredService<IConnectionCredentialUseGrantStore>();
        var grant = await store.FindAsync(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, granted, FixtureConstants.BindingId, cancellationToken);
        Require.That(grant is { IsActive: true } && grant.ConnectionId == connection && grant.BindingRevision == revision && grant.WorkflowInstanceId == granted, "grant-readback-not-exact");
        Require.That(await store.FindAsync(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, ungranted, FixtureConstants.BindingId, cancellationToken) == null, "ungranted-instance-has-grant");
        var policy = services.GetRequiredService<StoredConnectionCredentialBindingUseAuthorizer>();
        Require.That(await policy.AuthorizeAsync(new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, FixtureConstants.BindingId, connection, revision, granted), cancellationToken), "granted-policy-not-positive");
        Require.That(!await policy.AuthorizeAsync(new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, FixtureConstants.BindingId, connection, revision, ungranted), cancellationToken), "ungranted-policy-not-negative");
        Require.NoSecret(JsonSerializer.Serialize(grant));
    }

    private static async Task<RunWorkflowInstanceResponse> StartGrantWorkflowAsync(IWorkflowRuntime runtime, CancellationToken cancellationToken)
    {
        var client = await runtime.CreateClientAsync(cancellationToken);
        var response = await client.CreateAndRunInstanceAsync(new CreateAndRunWorkflowInstanceRequest
        {
            WorkflowDefinitionHandle = WorkflowDefinitionHandle.ByDefinitionId(GrantWorkflow.DefinitionId, VersionOptions.Latest)
        }, cancellationToken);
        Require.That(!response.CannotStart && response.SubStatus == WorkflowSubStatus.Suspended && response.Incidents.Count == 0, "real-grant-workflow-not-suspended");
        return response;
    }

    private static async Task AssertSuspendedAsync(IServiceProvider services, string instanceId, CancellationToken cancellationToken)
    {
        var persisted = await services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(instanceId, cancellationToken);
        Require.That(persisted is { SubStatus: WorkflowSubStatus.Suspended } && persisted.WorkflowState.Bookmarks.Count == 1, "grant-suspension-not-durable");
        Require.NoSecret(services.GetRequiredService<IWorkflowStateSerializer>().Serialize(persisted!.WorkflowState));
    }

    private static async Task<string> SingleBookmarkAsync(IServiceProvider services, string instanceId, CancellationToken cancellationToken)
    {
        var bookmarks = (await services.GetRequiredService<IBookmarkStore>().FindManyAsync(new() { WorkflowInstanceId = instanceId }, cancellationToken)).ToArray();
        Require.That(bookmarks.Length == 1, "durable-bookmark-count");
        return bookmarks[0].Id;
    }
}
