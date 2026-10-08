using System.Text;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Persistence.EFCore;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Persistence;
using Elsa.Slack.SocketMode.Events;
using Elsa.Tenants.Options;
using Elsa.Workflows;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.WorkerProcess;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime;
using Elsa.Workflows.Runtime.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class SocketDurableWorkTests(PostgreSqlConnectionsFixture fixture)
{
    private static readonly TimeSpan Deadline = TimeSpan.FromSeconds(30);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StartupDiscoveryExecutesAdmittedAndMaterializedWorkWithoutANotificationButNeverAutoResumes(bool shell)
    {
        await RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var first = await host.AdmitAsync("startup-admitted");
            var second = await host.AdmitAsync("startup-materialized");
            await AdmissionRuntimeTestFixture.MaterializeAsync(new(host.Services, host.Probe, host.Execution, host.Store, second));
            var accessor = host.Services.GetRequiredService<ITenantAccessor>();
            using (accessor.PushContext(new Tenant { Id = "other-caller-tenant", Name = "Caller" }))
            {
                var batch = await host.RunBatchAsync();
                Assert.Equal(2, batch.RecoveryExamined);
                Assert.Equal(2, batch.ExecutionAttempts);
                Assert.Equal(2, batch.Executed);
                Assert.False(batch.Uncertain);
                Assert.False(batch.RecoveryScanCompleted);
                Assert.Equal("other-caller-tenant", accessor.TenantId);
            }
            Assert.Equal(AdmissionState.ExecutionObserved, (await host.Store.FindAsync(first))!.State);
            Assert.Equal(AdmissionState.ExecutionObserved, (await host.Store.FindAsync(second))!.State);
            Assert.Equal(2, host.Probe.Count("activityEffects"));
            Assert.True((await host.RunBatchAsync()).RecoveryScanCompleted);
            var revisited = await host.RunBatchAsync();
            Assert.Equal(2, revisited.Skipped);
            Assert.Equal(0, revisited.ExecutionAttempts);
            Assert.Equal(0, revisited.RecoveryAttempts);
            Assert.Equal(0, host.Probe.Count("activityResumes"));
            var instanceId = (await host.Store.FindAsync(first))!.WorkflowInstanceId!;
            var instance = (await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(instanceId))!;
            var client = await host.Services.GetRequiredService<IWorkflowRuntime>().CreateClientAsync(instanceId);
            Assert.Equal(WorkflowSubStatus.Finished, (await client.RunInstanceAsync(new RunWorkflowInstanceRequest
            {
                BookmarkId = Assert.Single(instance.WorkflowState.Bookmarks).Id
            })).SubStatus);
            Assert.Equal(1, host.Probe.Count("activityResumes"));
            Assert.Equal(2, host.Probe.Count("activityEffects"));
        }, shell: shell, pageSize: 2);
    }

    [Fact]
    public async Task ARealHeldOwnerCannotBeRecoveredOrReenteredByTheScanner()
    {
        await RunAsync(async host =>
        {
            host.Probe.Outcome = "suspended";
            var admissionId = await host.AdmitAsync("live-owner");
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.StartPrepared))
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(Deadline);
                }
            };
            var execution = host.Execution.ExecuteAsync(admissionId);
            try
            {
                await entered.Task.WaitAsync(Deadline);
                var before = (await host.Store.FindAsync(admissionId))!;
                Assert.Equal(AdmissionState.StartPreparing, before.State);
                var batch = await host.RunBatchAsync();
                Assert.Equal(1, batch.Skipped);
                Assert.Equal(0, batch.ExecutionAttempts);
                Assert.Equal(0, batch.RecoveryAttempts);
                Assert.False(batch.Uncertain);
                var after = (await host.Store.FindAsync(admissionId))!;
                Assert.Equal(before.Revision, after.Revision);
                Assert.Equal(before.State, after.State);
                Assert.Equal(0, host.Probe.Count("activityEffects"));
            }
            finally
            {
                release.TrySetResult();
                await execution.WaitAsync(Deadline);
            }
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(AdmissionState.ExecutionObserved, (await host.Store.FindAsync(admissionId))!.State);
            Assert.Equal(1, (await host.RunBatchAsync()).Skipped);
            Assert.Equal(0, host.Probe.Count("activityResumes"));
        });
    }

    [Theory]
    [InlineData(AdmissionState.Creating, false)]
    [InlineData(AdmissionState.StartPreparing, false)]
    [InlineData(AdmissionState.StartAuthorized, false)]
    [InlineData(AdmissionState.StartAuthorized, true)]
    public async Task RestartDiscoveryClassifiesUnknownBoundariesWithoutRepeatingCreationOrIssuingAuthority(AdmissionState state, bool withdrawn)
    {
        await RunAsync(async host =>
        {
            var admissionId = await host.AdmitAsync("unknown-" + state);
            AdmissionRecord record;
            if (state == AdmissionState.Creating)
            {
                var admitted = (await host.Store.FindAsync(admissionId))!;
                record = (await host.Store.BeginCreationAsync(admitted.Id, admitted.Revision, Guid.NewGuid().ToString("N")))!;
            }
            else
            {
                await AdmissionRuntimeTestFixture.MaterializeAsync(new(host.Services, host.Probe, host.Execution, host.Store, admissionId));
                var materialized = (await host.Store.FindAsync(admissionId))!;
                record = (await host.Store.PrepareStartAsync(materialized.Id, materialized.Revision, "synthetic-restarted-attempt", null, null))!;
                if (state == AdmissionState.StartAuthorized)
                {
                    record = (await host.Store.AuthorizeStartAsync(record.Id, record.Revision, record.AttemptId!))!;
                }
            }
            Assert.Equal(state, record.State);
            if (withdrawn)
            {
                var subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
                Assert.NotNull(await host.Store.WithdrawAsync(subscription.Id, subscription.Revision, false, null));
            }
            var insertions = host.Probe.Count("instanceInsertAttempts");
            var notifications = host.Probe.Count("savedNotifications");
            var batch = await host.RunBatchAsync();
            Assert.Equal(1, batch.RecoveryAttempts);
            Assert.Equal(0, batch.ExecutionAttempts);
            Assert.False(batch.Uncertain);
            var recovered = (await host.Store.FindAsync(admissionId))!;
            Assert.Equal(AdmissionState.RecoveryRequired, recovered.State);
            Assert.Equal(state == AdmissionState.StartAuthorized, recovered.AuthorityOutstanding);
            Assert.True(recovered.Revision > record.Revision);
            Assert.Equal(insertions, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(notifications, host.Probe.Count("savedNotifications"));
            Assert.Equal(0, host.Probe.Count("AuthorityConsumed"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            Assert.Equal(1, (await host.RunBatchAsync()).Skipped);
        });
    }

    [Fact]
    public async Task WrongEpochUnconfiguredSubscriptionAndWithdrawnBindingCannotDispatch()
    {
        await RunAsync(async host =>
        {
            var own = await host.AdmitAsync("captured-binding");
            var other = host.Configuration.Subscriptions[0].Configuration with { Id = "unconfigured-subscription" };
            await AdmissionTestLedger.ActivateAsync(host.Store, other);
            var foreign = await host.Store.AdmitAsync(AdmissionWorkerHost.Event("unconfigured-event", other.Id), AdmissionWorkerHost.Now);
            Assert.Equal(AdmissionOutcome.Committed, foreign.Outcome);
            var captured = host.Configuration.Subscriptions[0];
            var wrong = host.WithSubscriptions([captured with { ActivationEpoch = captured.ActivationEpoch + 1 }]);
            var work = new SlackSocketDurableWork(wrong, host.Scopes, host.Time);
            var skipped = 0;
            for (var i = 0; i < 3; i++)
            {
                var batch = await host.RunBatchAsync(work);
                skipped += batch.Skipped;
                Assert.False(batch.Uncertain);
                Assert.Equal(0, batch.ExecutionAttempts);
            }
            Assert.Equal(2, skipped);
            var current = (await host.Store.FindSubscriptionAsync(captured.Configuration.Id))!;
            Assert.NotNull(await host.Store.WithdrawAsync(current.Id, current.Revision, false, null));
            var withdrawn = await host.RunBatchAsync();
            Assert.Equal(1, withdrawn.Skipped);
            Assert.Equal(0, withdrawn.ExecutionAttempts);
            Assert.Equal(AdmissionState.Admitted, (await host.Store.FindAsync(own))!.State);
            Assert.Equal(AdmissionState.Admitted, (await host.Store.FindAsync(foreign.AdmissionId!))!.State);
            Assert.Equal(0, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
        }, pageSize: 1);
    }

    [Theory]
    [InlineData("malformed", false)]
    [InlineData("other-binding", true)]
    public async Task MalformedOrDifferentListenerPayloadCannotReachExecutionOrMutateItsRecord(string scenario, bool materialized)
    {
        await RunAsync(async host =>
        {
            var admitted = scenario == "malformed"
                ? await host.Execution.AdmitAsync(AdmissionWorkerHost.Event("malformed-event") with { Payload = "not-a-normalized-Socket-event" })
                : await host.Execution.AdmitAsync(host.Message("other-binding-event"));
            Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
            if (materialized)
            {
                await AdmissionRuntimeTestFixture.MaterializeAsync(new(host.Services, host.Probe, host.Execution, host.Store, admitted.AdmissionId!));
            }
            var before = (await host.Store.FindAsync(admitted.AdmissionId!))!;
            var insertions = host.Probe.Count("instanceInsertAttempts");
            var notifications = host.Probe.Count("savedNotifications");
            var wrongBinding = host.WithSubscriptions(host.Configuration.Subscriptions, "other-trusted-connection");
            var batch = await host.RunBatchAsync(scenario == "malformed" ? host.Work : new(wrongBinding, host.Scopes, host.Time));
            Assert.True(batch.Uncertain);
            Assert.Equal(1, batch.RecoveryExamined);
            Assert.Equal(0, batch.ExecutionAttempts);
            Assert.Equal(0, batch.Executed);
            var after = (await host.Store.FindAsync(before.Id))!;
            Assert.Equal(before.Revision, after.Revision);
            Assert.Equal(before.State, after.State);
            Assert.Equal(before.Payload, after.Payload);
            Assert.Equal(before.CheckpointFingerprint, after.CheckpointFingerprint);
            Assert.Equal(insertions, host.Probe.Count("instanceInsertAttempts"));
            Assert.Equal(notifications, host.Probe.Count("savedNotifications"));
            Assert.Equal(0, host.Probe.Count("workflowExecuting"));
            Assert.Equal(0, host.Probe.Count("AuthorityConsumed"));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
        });
    }

    [Fact]
    public async Task TerminalWorkflowDiscoveryAndReceiptCleanupUseActualStoresAndKeepOwnedCapacityCharged()
    {
        await RunAsync(async host =>
        {
            var admissionId = await host.AdmitAsync("terminal-owned");
            var result = (await host.Execution.ExecuteAsync(admissionId))!;
            var record = (await host.Store.FindAsync(admissionId))!;
            Assert.Equal(AdmissionState.Terminal, record.State);
            var captured = host.Configuration.Subscriptions[0];
            var discarded = await host.Discards.RecordDiscardAsync(new(AdmissionWorkerHost.Event("terminal-discard") with
            {
                IsHumanMessage = false, Payload = "synthetic-normalized-bot"
            }, SlackSocketDiscardReason.Bot, host.Configuration.BindingFingerprint,
                captured.Configuration.ConfigurationFingerprint, captured.ActivationEpoch), AdmissionWorkerHost.Now);
            Assert.Equal(SlackSocketDiscardOutcome.Committed, discarded.Outcome);
            Assert.Empty((await host.Execution.ListRecoveryAsync(10)).Items);
            host.Time.Now = record.TerminalAt!.Value + captured.Configuration.Policy.IdentityHorizon;
            var batch = await host.RunBatchAsync();
            Assert.Equal(0, batch.RecoveryExamined);
            Assert.Equal(1, batch.TerminalExamined);
            Assert.Equal(1, batch.WorkflowCleaned);
            Assert.Equal(1, batch.ReceiptExamined);
            Assert.Equal(1, batch.ReceiptsRemoved);
            Assert.False(batch.Uncertain);
            var retained = (await host.Store.FindByInstanceAsync(result.WorkflowInstanceId))!;
            Assert.Null(retained.IdentityHash);
            Assert.Null(retained.ProviderEventId);
            Assert.Null(retained.Payload);
            Assert.Equal(record.EventFingerprint, retained.EventFingerprint);
            Assert.Equal(record.PayloadFingerprint, retained.PayloadFingerprint);
            Assert.NotNull(await host.Services.GetRequiredService<IWorkflowInstanceManager>().FindByIdAsync(result.WorkflowInstanceId));
            var subscription = (await host.Store.FindSubscriptionAsync(record.SubscriptionId))!;
            Assert.Equal(0, subscription.ActiveReservations);
            Assert.Equal(1, subscription.RetainedRecords);
            await using var receipts = await host.Services.GetRequiredService<IDbContextFactory<SlackSocketReceiptElsaDbContext>>().CreateDbContextAsync();
            Assert.Empty(await receipts.Receipts.ToListAsync());
            var next = await host.RunBatchAsync();
            Assert.Equal(0, next.TerminalExamined);
            Assert.Equal(0, next.WorkflowCleaned);
            Assert.Equal(0, next.ReceiptsRemoved);
        });
    }

    [Theory]
    [InlineData("reactivated", true)]
    [InlineData("reconfigured-same-authority", true)]
    [InlineData("reconfigured-different-authority", false)]
    public async Task HistoricalTerminalCleanupUsesStableNamespaceAndHistoricalPolicyButOnlyCurrentApprovedAuthority(string scenario, bool cleanupAllowed)
    {
        await RunAsync(async host =>
        {
            var admissionId = await host.AdmitAsync("historical-terminal");
            var executed = (await host.Execution.ExecuteAsync(admissionId))!;
            var historical = (await host.Store.FindAsync(admissionId))!;
            Assert.Equal(AdmissionState.Terminal, historical.State);
            var old = (await host.Store.FindSubscriptionAsync(historical.SubscriptionId))!;
            var current = (await host.Store.WithdrawAsync(old.Id, old.Revision, false, null))!;
            Assert.False(current.Active);
            if (scenario != "reactivated")
            {
                var configuration = current.Configuration with
                {
                    Policy = current.Configuration.Policy with
                    {
                        PayloadRetention = current.Configuration.Policy.PayloadRetention + TimeSpan.FromHours(1),
                        IdentityHorizon = current.Configuration.Policy.IdentityHorizon + TimeSpan.FromDays(1),
                        CleanupAuthority = cleanupAllowed ? current.Configuration.Policy.CleanupAuthority : "different-current-approved-authority"
                    }
                };
                current = (await host.Store.ReconfigureAsync(configuration, current.Revision))!;
                Assert.False(current.BootstrapVerified);
                // Actual ledger configuration/activation transitions; this does not start a
                // second runtime or claim that the old host's execution allowlist was changed.
                current = (await host.Store.VerifyBootstrapAsync(current.Id, current.Revision, configuration.ConfigurationFingerprint))!;
                Assert.NotEqual(historical.ConfigurationFingerprint, current.ConfigurationFingerprint);
            }
            current = (await host.Store.ActivateAsync(current.Id, current.Revision, AdmissionWorkerHost.Now))!;
            Assert.True(current.Active);
            Assert.True(current.ActivationEpoch > historical.ActivationEpoch);
            var selected = new SlackSocketSubscription(current.Configuration, current.ActivationEpoch);
            var work = new SlackSocketDurableWork(host.WithSubscriptions([selected]), host.Scopes, host.Time);
            host.Time.Now = historical.TerminalAt!.Value + old.Configuration.Policy.IdentityHorizon;
            var batch = await host.RunBatchAsync(work);
            Assert.Equal(1, batch.TerminalExamined);
            Assert.Equal(cleanupAllowed ? 1 : 0, batch.WorkflowCleaned);
            Assert.Equal(0, batch.ExecutionAttempts);
            Assert.False(batch.Uncertain);
            var retained = (await host.Store.FindByInstanceAsync(executed.WorkflowInstanceId))!;
            Assert.Equal(historical.AdmittedConfigurationJson, retained.AdmittedConfigurationJson);
            Assert.Equal(historical.ConfigurationFingerprint, retained.ConfigurationFingerprint);
            Assert.Equal(historical.ActivationEpoch, retained.ActivationEpoch);
            Assert.Equal(historical.EventFingerprint, retained.EventFingerprint);
            if (cleanupAllowed)
            {
                Assert.Null(retained.Payload);
                Assert.Null(retained.ProviderEventId);
                Assert.Null(retained.IdentityHash);
                Assert.True(retained.Revision > historical.Revision);
            }
            else
            {
                Assert.Equal(historical.Payload, retained.Payload);
                Assert.Equal(historical.ProviderEventId, retained.ProviderEventId);
                Assert.Equal(historical.IdentityHash, retained.IdentityHash);
                Assert.Equal(historical.Revision, retained.Revision);
            }
            Assert.Equal(1, (await host.Store.FindSubscriptionAsync(current.Id))!.RetainedRecords);
            Assert.Equal(0, (await host.Store.FindSubscriptionAsync(current.Id))!.ActiveReservations);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            Assert.Equal(0, host.Probe.Count("activityResumes"));
        });
    }

    [Fact]
    public async Task IneligibleTerminalPagesAdvanceAndCompletedScansRevisitThemAndLaterInsertions()
    {
        await RunAsync(async host =>
        {
            for (var i = 0; i < 3; i++)
            {
                await host.SuppressAsync("ineligible-" + i);
            }
            var examined = 0;
            for (var i = 0; i < 4; i++)
            {
                var batch = await host.RunBatchAsync();
                Assert.InRange(batch.TerminalExamined, 0, 1);
                Assert.Equal(0, batch.WorkflowCleaned);
                Assert.False(batch.Uncertain);
                examined += batch.TerminalExamined;
                Assert.Equal(i == 3, batch.TerminalScanCompleted);
            }
            Assert.Equal(3, examined);
            host.Time.Now += host.Configuration.Subscriptions[0].Configuration.Policy.IdentityHorizon;
            var cleaned = 0;
            for (var i = 0; i < 4; i++)
            {
                cleaned += (await host.RunBatchAsync()).WorkflowCleaned;
            }
            Assert.Equal(3, cleaned);
            Assert.Equal(0, (await host.Store.FindSubscriptionAsync(host.Configuration.Subscriptions[0].Configuration.Id))!.RetainedRecords);
            var inserted = await host.SuppressAsync("later-insertion");
            Assert.Equal(1, (await host.RunBatchAsync()).WorkflowCleaned);
            Assert.Null(await host.Store.FindAsync(inserted));
            Assert.Equal(0, host.Probe.Count("activityEffects"));
        }, pageSize: 1);
    }

    [Fact]
    public async Task AFailedAdmissionDoesNotStarveLaterWorkOrReplayItsUnknownEffects()
    {
        await RunAsync(async host =>
        {
            var first = await host.AdmitAsync("first-failure");
            var second = await host.AdmitAsync("second-success");
            var failed = 0;
            host.Probe.Boundary = boundary => boundary == nameof(AdmissionExecutionBoundary.CreationClaimed) && Interlocked.Exchange(ref failed, 1) == 0
                ? throw new IOException("synthetic-unknown-creation") : Task.CompletedTask;
            var batch = await host.RunBatchAsync();
            Assert.True(batch.Uncertain);
            Assert.Equal(2, batch.ExecutionAttempts);
            Assert.Equal(1, batch.Executed);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
            var records = new[] { (await host.Store.FindAsync(first))!, (await host.Store.FindAsync(second))! };
            Assert.Single(records, x => x.State == AdmissionState.RecoveryRequired);
            Assert.Single(records, x => x.State == AdmissionState.Terminal);
            await host.RunBatchAsync();
            var revisited = await host.RunBatchAsync();
            Assert.Equal(0, revisited.ExecutionAttempts);
            Assert.Equal(1, revisited.Skipped);
            Assert.Equal(1, host.Probe.Count("activityEffects"));
        }, pageSize: 2);
    }

    [Fact]
    public async Task OverlapAndCancellationCannotCreateAnotherExecutorAndTheActualUnwoundBatchCanBeReused()
    {
        await RunAsync(async host =>
        {
            await host.AdmitAsync("canceled-owner");
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            host.Probe.Boundary = async boundary =>
            {
                if (boundary == nameof(AdmissionExecutionBoundary.BeforeRunnerEntry))
                {
                    entered.TrySetResult();
                    await release.Task.WaitAsync(Deadline);
                }
            };
            await using var canceled = new SlackSocketOperation(() => Assert.Fail("Cancellation callbacks must settle without failure."), _ => { });
            var running = host.Work.RunBatchAsync(canceled);
            try
            {
                await entered.Task.WaitAsync(Deadline);
                await Assert.ThrowsAsync<InvalidOperationException>(() => host.RunBatchAsync());
                await canceled.RequestCancellation();
            }
            finally
            {
                release.TrySetResult();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running.WaitAsync(Deadline));
            }
            Assert.Equal(0, host.Probe.Count("activityEffects"));
            var next = await host.RunBatchAsync();
            Assert.Equal(0, next.ExecutionAttempts);
            Assert.False(next.Uncertain);
            await using var before = new SlackSocketOperation(() => Assert.Fail("Cancellation callbacks must settle without failure."), _ => { });
            await before.RequestCancellation();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.Work.RunBatchAsync(before));
        });
    }

    private async Task RunAsync(Func<DurableFixture, Task> assertion, bool shell = false, int pageSize = 4)
    {
        await fixture.ResetSchemaAsync();
        var probe = new AdmissionRuntimeProbe(fixture.ConnectionString);
        await using var services = AdmissionRuntimeHost.CreateServices(fixture.ConnectionString, probe, shell: shell, configure: collection =>
        {
            collection.Configure<TenantsOptions>(options => options.IsEnabled = true);
            collection.AddDbContextFactory<SlackSocketReceiptElsaDbContext>((_, builder) => builder.UseElsaPostgreSql(
                typeof(SlackSocketReceiptElsaDbContext).Assembly, fixture.ConnectionString,
                new ElsaDbContextOptions { MigrationsHistoryTableName = SlackSocketReceiptElsaDbContext.HistoryTable }));
            collection.AddSlackSocketDiscardPersistence();
        });
        using var tenant = AdmissionRuntimeHost.EnterTenant(services);
        await using (var admission = await services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync())
        {
            Assert.True(admission.IsTenantFilteringEnabled);
            Assert.Equal(AdmissionWorkerHost.TenantId, admission.TenantId);
            SlackSocketModeHostValidator.DemandDatabaseLayout(admission, ElsaDbContextBase.MigrationsHistoryTable);
        }
        await AdmissionRuntimeHost.MigrateAsync(services);
        await using (var receipts = await services.GetRequiredService<IDbContextFactory<SlackSocketReceiptElsaDbContext>>().CreateDbContextAsync())
        {
            await receipts.Database.MigrateAsync();
        }
        var subscription = await AdmissionRuntimeHost.BootstrapAsync(services);
        var captured = new SlackSocketSubscription(subscription.Configuration, subscription.ActivationEpoch);
        var limits = new SlackSocketModeLimits(65536, 16, TimeSpan.FromSeconds(5), 16, pageSize, pageSize, 1, 4, 3,
            TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        var configuration = new SlackSocketModeConfiguration(AdmissionWorkerHost.TenantId, AdmissionWorkerHost.EnvironmentId,
            captured.Configuration.InstallationId, "synthetic-connection", "A_FIXTURE", "T_FIXTURE", null, "U_SELF",
            captured.Configuration.ChannelId, [captured], limits);
        await assertion(new(services, probe, configuration));
    }

    private sealed class DurableFixture
    {
        internal DurableFixture(IServiceProvider services, AdmissionRuntimeProbe probe, SlackSocketModeConfiguration configuration)
        {
            Services = services;
            Probe = probe;
            Configuration = configuration;
            Work = new(configuration, Scopes, Time);
        }
        internal IServiceProvider Services { get; }
        internal AdmissionRuntimeProbe Probe { get; }
        internal SlackSocketModeConfiguration Configuration { get; }
        internal IServiceScopeFactory Scopes => Services.GetRequiredService<IServiceScopeFactory>();
        internal AdmissionExecutionService Execution => Services.GetRequiredService<AdmissionExecutionService>();
        internal IAdmissionStore Store => Services.GetRequiredService<IAdmissionStore>();
        internal ISlackSocketDiscardStore Discards => Services.GetRequiredService<ISlackSocketDiscardStore>();
        internal FixtureTime Time { get; } = new();
        internal SlackSocketDurableWork Work { get; }
        internal async Task<SlackSocketDurableWorkBatch> RunBatchAsync(SlackSocketDurableWork? work = null)
        {
            await using var operation = new SlackSocketOperation(() => Assert.Fail("Cancellation callbacks must settle without failure."), _ => { });
            return await (work ?? Work).RunBatchAsync(operation);
        }
        internal async Task<string> AdmitAsync(string eventId)
        {
            var result = await Execution.AdmitAsync(Message(eventId));
            Assert.Equal(AdmissionOutcome.Committed, result.Outcome);
            return result.AdmissionId!;
        }
        internal AdmissionEvent Message(string eventId)
        {
            var envelope = JsonSerializer.Serialize(new
            {
                type = "events_api", envelope_id = "synthetic-envelope",
                payload = new
                {
                    type = "event_callback", api_app_id = Configuration.ExpectedAppId, team_id = Configuration.ExpectedTeamId,
                    event_id = eventId, event_time = AdmissionWorkerHost.Now.ToUnixTimeSeconds(),
                    @event = new { type = "message", channel = Configuration.ChannelId, user = "U_HUMAN", text = "synthetic-human-message", ts = "1700000000.000001" }
                }
            });
            var payload = new SlackSocketWireParser(Configuration).Parse(Encoding.UTF8.GetBytes(envelope)).Event!;
            return AdmissionWorkerHost.Event(eventId) with { Payload = JsonSerializer.Serialize(payload) };
        }
        internal async Task<string> SuppressAsync(string eventId)
        {
            var id = await AdmitAsync(eventId);
            var record = (await Store.FindAsync(id))!;
            Assert.NotNull(await Execution.ResolveAsync(id, record.Revision, AdmissionTerminalDisposition.SuppressedBeforeStart,
                "synthetic-effect-free-admission", true, true));
            return id;
        }
        internal SlackSocketModeConfiguration WithSubscriptions(IReadOnlyList<SlackSocketSubscription> subscriptions, string? connectionId = null) => new(
            Configuration.TenantId, Configuration.EnvironmentId, Configuration.InstallationId, connectionId ?? Configuration.ConnectionId,
            Configuration.ExpectedAppId, Configuration.ExpectedTeamId, Configuration.ExpectedEnterpriseId, Configuration.SelfUserId,
            Configuration.ChannelId, subscriptions, Configuration.Limits);
    }

    private sealed class FixtureTime : TimeProvider
    {
        internal DateTimeOffset Now { get; set; } = AdmissionWorkerHost.Now;
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
