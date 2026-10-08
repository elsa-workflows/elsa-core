using System.Text.Json;
using Elsa.Connections.Contracts;
using Elsa.Connections.Credentials.Persistence.EFCore;
using Elsa.Connections.Credentials.Workflows.Contracts;
using Elsa.Connections.Credentials.Workflows.Services;
using Elsa.Connections.Models;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Secrets.Models;
using Elsa.Secrets.Persistence.EFCore;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Runtime;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace SocketPackageConsumer;

internal static class Scenarios
{
    internal const string CaseId = "socket-package-held-commit-watch-resume";

    internal static async Task<ScenarioEvidence> RunAsync(ConsumerOptions options, CancellationToken cancellationToken)
    {
        var database = await Migrations.DatabaseAsync(options.Connection, cancellationToken);
        var lifetimes = new HostLifetimes();
        var binding = await ConsumerHost.CompileBindingAsync(cancellationToken);
        string connectionId;
        // Nonexecuting setup controller. It is disposed before any Admission host exists.
        await using (var controller = ConsumerHost.Create(options.Connection, options.Feature, false, lifetimes, binding))
        {
            using var tenant = controller.EnterTenant();
            await using var scope = controller.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await Migrations.ApplyAsync(services, false, cancellationToken);
            await controller.StartAsync(cancellationToken);
            var connected = await services.GetRequiredService<IStaticApiKeyLifecycleService>().ConnectApiKeyAsync(FixturePolicies.Principal(),
                new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "fixture-provider", "fixture-account", FixtureConstants.SecretMarker), cancellationToken);
            Require.That(connected.Succeeded && connected.ConnectionId != null, "managed-api-key-not-created");
            connectionId = connected.ConnectionId!;
            ((FixturePolicies)services.GetRequiredService<IConnectionUseAuthorizer>()).ConnectionId = connectionId;
            var createdBinding = await services.GetRequiredService<IWorkflowCredentialBindingManager>().CreateAsync(FixturePolicies.Principal(),
                FixtureConstants.BindingId, connectionId, cancellationToken);
            Require.That(createdBinding.Succeeded && createdBinding.Revision.HasValue, "credential-binding-not-created");
            var row = await services.GetRequiredService<IConnectionLifecycleStore>().FindAsync(connectionId,
                FixtureConstants.TenantId, FixtureConstants.EnvironmentId, cancellationToken);
            Require.That(row is { Status: ConnectionStatus.Active, CurrentGenerationId: not null, CurrentSecretName: not null }, "managed-generation-not-active");
            await using var secrets = await services.GetRequiredService<IDbContextFactory<SecretsElsaDbContext>>().CreateDbContextAsync(cancellationToken);
            var secret = await secrets.Secrets.SingleAsync(x => x.Name == row!.CurrentSecretName, cancellationToken);
            Require.That(secret.ManagedOwnerId == connectionId && secret.ManagedGenerationId == row!.CurrentGenerationId, "managed-generation-not-bound");
            var encrypted = secrets.Entry(secret).Property<string>(SecretShadowPropertyNames.SerializedVersions).CurrentValue;
            Require.That(encrypted != null && encrypted.Contains("protectedValue", StringComparison.Ordinal), "managed-secret-not-encrypted");
            Require.NoSecret(encrypted!);
            Require.That(controller.Probe.Executing == 0 && controller.Probe.Provider.Calls == 0, "setup-executed-or-called-provider");
        }

        AdmissionSubscription active;
        // Real guarded inactive bootstrap/publication, then definite activation, no event execution.
        await using (var bootstrapHost = ConsumerHost.Create(options.Connection, options.Feature, true, lifetimes, binding))
        {
            using var tenant = bootstrapHost.EnterTenant();
            await using var scope = bootstrapHost.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await Migrations.ApplyAsync(services, true, cancellationToken);
            await bootstrapHost.StartAsync(cancellationToken);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync(cancellationToken);
            var bootstrap = services.GetRequiredService<AdmissionBootstrapService>();
            var inactive = await bootstrap.ProvisionAsync(binding.Configuration, binding.Artifact, cancellationToken);
            Require.That(!inactive.Active && inactive.BootstrapVerified && !inactive.Retired, "inactive-bootstrap-not-verified");
            active = await bootstrap.ActivateAsync(inactive.Id, inactive.Revision, cancellationToken)
                ?? throw new ProofFailure("definite-activation-missing");
            var persisted = await services.GetRequiredService<IAdmissionStore>().FindSubscriptionAsync(active.Id, cancellationToken);
            Require.That(persisted is { Active: true, BootstrapVerified: true } && persisted.ActivationEpoch == active.ActivationEpoch &&
                persisted.ConfigurationFingerprint == binding.Configuration.ConfigurationFingerprint && bootstrapHost.Probe.Executing == 0,
                "activation-not-durable-or-executed");
        }

        var configuration = new SlackSocketModeConfiguration(FixtureConstants.TenantId, FixtureConstants.EnvironmentId,
            binding.Configuration.InstallationId, connectionId, "A_TEST", "T_TEST", null, FixtureConstants.SelfUserId, FixtureConstants.ChannelId,
            [new(binding.Configuration, active.ActivationEpoch)], new(65536, 16, TimeSpan.FromSeconds(20), 32, 4, 4, 1, 1, 1,
                TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(90), TimeSpan.FromSeconds(20)));
        var clock = new EvidenceClock();
        var commit = new AdmissionCommitGate(clock);
        var peer = new LoopbackPeer(clock);
        MigrationEvidence[] migrations = [];
        Dictionary<string, int> counters = new();
        Dictionary<string, string> correlation = new();
        try
        {
            await using var host = ConsumerHost.Create(options.Connection, options.Feature, true, lifetimes, binding, configuration, peer, commit);
            using var tenant = host.EnterTenant();
            await using var scope = host.Services.CreateAsyncScope();
            var services = scope.ServiceProvider;
            await Migrations.ApplyAsync(services, true, cancellationToken);
            await host.Probe.InitializeAsync(cancellationToken);
            await services.GetRequiredService<IRegistriesPopulator>().PopulateAsync(cancellationToken);
            await services.GetRequiredService<AdmissionHostConfiguration>().ValidateAsync(services, cancellationToken);
            Require.That(services.GetRequiredService<IWorkflowRuntime>().GetType() == typeof(AdmissionWorkflowRuntime) &&
                services.GetRequiredService<IWorkflowRunner>().GetType() == typeof(WorkflowRunner), "real-guarded-runtime-not-selected");
            // Startup validates actual provisioned histories, selected stores, binding and private listener authority.
            await host.StartAsync(cancellationToken);
            await peer.Connected.WaitAsync(cancellationToken);
            await peer.SendAsync("{\"type\":\"hello\"}", cancellationToken);
            commit.Arm();
            try
            {
                await peer.SendAsync(Envelope(), cancellationToken);
                await commit.Entered.Task.WaitAsync(cancellationToken);
                Require.That(peer.Acknowledgements == 0 && !peer.Acknowledged.Task.IsCompleted && commit.CommittedOrder == 0,
                    "ack-before-durable-commit");
                Require.That(host.Probe.Executing == 0 && host.Probe.Started == 0 && host.Probe.WatchExecutions == 0 &&
                    await host.Probe.CountAsync("watch", cancellationToken) == 0 && await host.Probe.CountAsync("suspend", cancellationToken) == 0,
                    "workflow-before-durable-commit");
                // A separate real connection/context cannot see an uncommitted identity or instance.
                await using (var ledger = await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync(cancellationToken))
                {
                    Require.That(!await ledger.Admissions.AsNoTracking().AnyAsync(cancellationToken), "uncommitted-admission-visible");
                }
                await using (var management = await services.GetRequiredService<IDbContextFactory<ManagementElsaDbContext>>().CreateDbContextAsync(cancellationToken))
                {
                    Require.That(!await management.WorkflowInstances.AsNoTracking().AnyAsync(cancellationToken), "instance-before-durable-commit");
                }
                commit.Release.TrySetResult();
                await Task.WhenAll(peer.Acknowledged.Task, host.Probe.ActivityEntered.Task).WaitAsync(cancellationToken);
                Require.That(commit.HeldCommits == 1 && commit.CommittedOrder > 0 && peer.AckOrder > commit.CommittedOrder &&
                    peer.Acknowledgements == 1, "physical-ack-commit-order");
                Require.That(!host.Probe.ReleaseActivity.Task.IsCompleted && await host.Probe.CountAsync("suspend", cancellationToken) == 0 &&
                    services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount == 1, "ack-blocked-by-workflow-duration");
                Require.That(services.GetRequiredService<ISlackSocketModeHealth>().GetSnapshot().State == SlackSocketModeHealthState.Connected,
                    "listener-not-connected");
                host.Probe.ReleaseActivity.TrySetResult();
                await host.Probe.CycleSettled.Task.WaitAsync(cancellationToken);
                var store = services.GetRequiredService<IAdmissionStore>();
                AdmissionRecord record;
                await using (var ledger = await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync(cancellationToken))
                {
                    record = await ledger.Admissions.AsNoTracking().SingleAsync(cancellationToken);
                }
                Require.That(record.Id == commit.HeldAdmissionId && record.State == AdmissionState.ExecutionObserved && !record.AuthorityOutstanding && record.WorkflowInstanceId != null &&
                    record.ActivationEpoch == active.ActivationEpoch && record.ConfigurationFingerprint == binding.Configuration.ConfigurationFingerprint,
                    "real-suspension-checkpoint-missing");
                var manager = services.GetRequiredService<IWorkflowInstanceManager>();
                var instance = await manager.FindByIdAsync(record.WorkflowInstanceId!, cancellationToken) ?? throw new ProofFailure("instance-missing");
                Require.That(instance.SubStatus == WorkflowSubStatus.Suspended && instance.WorkflowState.Incidents.Count == 0 &&
                    instance.WorkflowState.Bookmarks.Count == 1, "suspension-not-persisted");
                CheckOutputs(instance);
                var bookmark = instance.WorkflowState.Bookmarks.Single().Id;
                var persistedBookmarks = (await services.GetRequiredService<IBookmarkStore>().FindManyAsync(new() { WorkflowInstanceId = instance.Id }, cancellationToken)).ToArray();
                Require.That(persistedBookmarks.Length == 1 && persistedBookmarks[0].Id == bookmark, "bookmark-not-persisted");
                var client = await services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(instance.Id, cancellationToken);
                var before = services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState);
                var executing = host.Probe.Executing;
                var started = host.Probe.Started;
                var context = host.Probe.LastContext ?? throw new ProofFailure("real-context-not-observed");
                var deniedEntries = await DeniedCountAsync(
                [
                    () => services.GetRequiredService<IWorkflowExecutionPipeline>().ExecuteAsync(context),
                    () => services.GetRequiredService<IWorkflowExecutionPipeline>().Pipeline(context).AsTask(),
                    () => services.GetRequiredService<IWorkflowRunner>().RunAsync(context),
                    () => client.RunInstanceAsync(new(), cancellationToken), () => client.CancelAsync(cancellationToken),
                    () => client.ImportStateAsync(instance.WorkflowState, cancellationToken), () => client.DeleteAsync(cancellationToken)
                ]);
                var deniedBackground = await DeniedCountAsync(
                [
                    () => services.GetRequiredService<IConnectionBackgroundUseService>().ResolveForUseAsync(FixtureConstants.TenantId,
                        FixtureConstants.EnvironmentId, connectionId, cancellationToken)
                ]);
                var deniedLifecycle = await DeniedCountAsync(
                [
                    () => services.GetRequiredService<IConnectionLifecycleService>().DisconnectAsync(FixturePolicies.Principal(),
                        FixtureConstants.TenantId, FixtureConstants.EnvironmentId, connectionId, cancellationToken),
                    () => services.GetRequiredService<IStaticApiKeyLifecycleService>().ConnectApiKeyAsync(FixturePolicies.Principal(),
                        new(FixtureConstants.TenantId, FixtureConstants.EnvironmentId, "fixture-provider", "fixture-account", FixtureConstants.SecretMarker), cancellationToken)
                ]);
                var credentialBinding = await services.GetRequiredService<IConnectionCredentialBindingStore>().FindAsync(FixtureConstants.TenantId,
                    FixtureConstants.EnvironmentId, FixtureConstants.BindingId, cancellationToken);
                Require.That(credentialBinding != null && credentialBinding.ConnectionId == connectionId &&
                    services.GetRequiredService<IWorkflowCredentialResolver>().GetType() == typeof(WorkflowCredentialResolver) &&
                    services.GetRequiredService<IConnectionCredentialUseGrantStore>().GetType() == typeof(EFCoreConnectionCredentialUseGrantStore) &&
                    !await services.GetRequiredService<StoredConnectionCredentialBindingUseAuthorizer>().AuthorizeAsync(new(FixtureConstants.TenantId,
                        FixtureConstants.EnvironmentId, FixtureConstants.BindingId, connectionId, credentialBinding.Revision, instance.Id), cancellationToken),
                    "actual-stored-no-grant-policy");
                var deniedOutbound = 0;
                try
                {
                    await services.GetRequiredService<IWorkflowCredentialResolver>().ResolveAsync(context, FixtureConstants.BindingId, cancellationToken);
                }
                catch (ConnectionUnavailableException)
                {
                    deniedOutbound++;
                }
                Require.That(deniedOutbound == 1 && await services.GetRequiredService<IConnectionCredentialUseGrantStore>().FindAsync(
                    FixtureConstants.TenantId, FixtureConstants.EnvironmentId, instance.Id, FixtureConstants.BindingId, cancellationToken) == null,
                    "implicit-outbound-authority");
                instance = (await manager.FindByIdAsync(instance.Id, cancellationToken))!;
                Require.That(host.Probe.Executing == executing && host.Probe.Started == started &&
                    services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState) == before &&
                    (await store.FindAsync(record.Id, cancellationToken))!.Revision == record.Revision, "denied-entry-effects");
                migrations = await Migrations.ReapplyPopulatedAsync(services, true, cancellationToken);
                var resumed = await client.RunInstanceAsync(new() { BookmarkId = bookmark }, cancellationToken);
                instance = (await manager.FindByIdAsync(instance.Id, cancellationToken))!;
                record = (await store.FindAsync(record.Id, cancellationToken))!;
                Require.That(resumed.Status == WorkflowStatus.Finished && instance.SubStatus == WorkflowSubStatus.Finished &&
                    instance.WorkflowState.Bookmarks.Count == 0 && instance.WorkflowState.Incidents.Count == 0 &&
                    record.State == AdmissionState.Terminal && record.TerminalDisposition == AdmissionTerminalDisposition.Completed &&
                    !record.AuthorityOutstanding && record.ActiveReservationReleased, "legitimate-resume-not-terminal");
                Require.That(!(await services.GetRequiredService<IBookmarkStore>().FindManyAsync(new() { WorkflowInstanceId = instance.Id }, cancellationToken)).Any(),
                    "bookmark-not-consumed");
                CheckOutputs(instance);
                Require.That(await host.Probe.CountAsync("watch", cancellationToken) == 1 && await host.Probe.CountAsync("suspend", cancellationToken) == 1 &&
                    await host.Probe.CountAsync("resume", cancellationToken) == 1 && host.Probe.Provider.Calls == 0 &&
                    services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount == 0, "execution-count-or-unwind");
                Require.NoSecret(services.GetRequiredService<IWorkflowStateSerializer>().Serialize(instance.WorkflowState));
                counters = new()
                {
                    ["heldAdmissionCommits"] = commit.HeldCommits, ["httpOpens"] = peer.OpenCalls, ["physicalAcknowledgements"] = peer.Acknowledgements,
                    ["watchExecutions"] = await host.Probe.CountAsync("watch", cancellationToken), ["suspensions"] = await host.Probe.CountAsync("suspend", cancellationToken),
                    ["resumes"] = await host.Probe.CountAsync("resume", cancellationToken), ["ownedEntryDenials"] = deniedEntries,
                    ["backgroundDenials"] = deniedBackground, ["lifecycleDenials"] = deniedLifecycle, ["outboundDenials"] = deniedOutbound,
                    ["providerCalls"] = host.Probe.Provider.Calls, ["activeExecutionCycles"] = services.GetRequiredService<IExecutionCycleRegistry>().ActiveCount
                };
                correlation = new()
                {
                    ["envelopeSha256"] = Hash.Text(FixtureConstants.EnvelopeId), ["eventSha256"] = Hash.Text(FixtureConstants.EventId),
                    ["admissionSha256"] = Hash.Text(record.Id), ["workflowInstanceSha256"] = Hash.Text(instance.Id), ["bookmarkSha256"] = Hash.Text(bookmark),
                    ["bindingSha256"] = configuration.BindingFingerprint
                };
            }
            finally
            {
                commit.Release.TrySetResult();
                host.Probe.ReleaseActivity.TrySetResult();
            }
        }
        finally
        {
            // Host stop must retire the physical client before the fixture tears down its server.
            // On failure the peer is still unconditionally released; no success report is emitted.
            try
            {
                await peer.ClientRetired.WaitAsync(TimeSpan.FromSeconds(20));
            }
            finally
            {
                await peer.DisposeAsync();
            }
        }
        Require.That(peer.Disposed && peer.ClientRetiredBeforeDisposal && peer.OpenCalls == 1 && peer.Acknowledgements == 1 && peer.ExactBearerObserved, "physical-peer-not-clean");
        var cleanup = lifetimes.Evidence(3);
        return new(CaseId, database.Identity, database.Version, new()
        {
            ["realManagedGenerationEncrypted"] = true, ["inactiveBootstrapVerified"] = true, ["definiteActivationCaptured"] = true,
            ["guardedRuntimeSelected"] = true, ["physicalHttpOpenAndHello"] = true, ["noAckBeforeCommit"] = true, ["noEffectsBeforeCommit"] = true,
            ["physicalAckAfterCommit"] = true, ["ackIndependentOfWorkflowDuration"] = true, ["realWatchOutputsPersisted"] = true,
            ["suspensionAndBookmarkPersisted"] = true, ["publicOwnedEntriesDeniedWithoutEffects"] = true,
            ["publicBackgroundAndLifecycleDenied"] = true, ["noImplicitOutboundGrant"] = true, ["actualStoredGrantPolicyDenied"] = true, ["legitimateResumeCompleted"] = true,
            ["terminalAndBookmarkConsumptionPersisted"] = true, ["migrationsReappliedPreserved"] = true, ["providerNotCalled"] = true,
            ["noSecretMarkers"] = true, ["physicalPeerDisposed"] = peer.Disposed, ["listenerRetiredPhysicalClient"] = peer.ClientRetiredBeforeDisposal
        }, counters, correlation, new(commit.CommittedOrder, peer.AckOrder), migrations, cleanup);
    }

    private static void CheckOutputs(WorkflowInstance instance)
    {
        var output = JsonSerializer.SerializeToElement(instance.WorkflowState.Output);
        Require.That(output.GetProperty("watchChannel").GetString() == FixtureConstants.ChannelId &&
            output.GetProperty("watchText").GetString() == FixtureConstants.MessageText && output.GetProperty("watchUser").GetString() == "U_HUMAN" &&
            output.GetProperty("watchTimestamp").GetString() == FixtureConstants.MessageTimestamp &&
            output.GetProperty("watchReplyThread").GetString() == FixtureConstants.MessageTimestamp && output.GetProperty("watchThreadAbsent").GetBoolean(),
            "watch-outputs-not-durable");
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

    private static string Envelope() => JsonSerializer.Serialize(new
    {
        type = "events_api", envelope_id = FixtureConstants.EnvelopeId,
        payload = new
        {
            type = "event_callback", api_app_id = "A_TEST", team_id = "T_TEST", event_id = FixtureConstants.EventId,
            event_time = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            @event = new { type = "message", channel = FixtureConstants.ChannelId, channel_type = "channel", user = "U_HUMAN", text = FixtureConstants.MessageText, ts = FixtureConstants.MessageTimestamp }
        }
    });
}
