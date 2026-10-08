using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class AdmissionPostgreSqlPersistenceTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task MigrationMatchesRuntimeModelAndReapplicationPreservesLedger()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        Assert.Single(db.Database.GetMigrations());
        Assert.False(db.Database.HasPendingModelChanges());
        var admission = await AdmissionTestLedger.Store(services).AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        await db.Database.MigrateAsync();
        Assert.NotNull(await AdmissionTestLedger.Store(services).FindAsync(admission.AdmissionId!));
        Assert.Single(await db.Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task IdentityFanOutAndChangedDuplicatePreserveOriginalBinding()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var first = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var duplicate = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now.AddSeconds(1));
        Assert.Equal(AdmissionOutcome.Committed, first.Outcome);
        Assert.Equal(first.AdmissionId, duplicate.AdmissionId);
        Assert.Equal(AdmissionOutcome.Duplicate, duplicate.Outcome);
        Assert.Equal(AdmissionOutcome.Quarantined, (await store.AdmitAsync(AdmissionWorkerHost.Event() with { Payload = "edited-same-provider-event" }, AdmissionWorkerHost.Now)).Outcome);
        var fanout = AdmissionWorkerHost.Configuration(id: "independent-subscription");
        await AdmissionTestLedger.ActivateAsync(store, fanout);
        var second = await store.AdmitAsync(AdmissionWorkerHost.Event(subscriptionId: fanout.Id), AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Committed, second.Outcome);
        Assert.NotEqual(first.AdmissionId, second.AdmissionId);
        Assert.Equal(1, (await store.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!.ActiveReservations);
    }

    [Fact]
    public async Task ForeignScopeOwnershipNeverAppearsUnowned()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        await store.BeginCreationAsync(admitted.AdmissionId!, admitted.Revision!.Value, "globally-owned-instance");
        await using var foreign = AdmissionWorkerHost.CreateServices(fixture.ConnectionString, new("foreign-tenant", AdmissionWorkerHost.EnvironmentId));
        var foreignStore = AdmissionTestLedger.Store(foreign);
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.FindByInstanceAsync("globally-owned-instance"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.ProvisionAsync(AdmissionWorkerHost.Configuration()));
        Assert.Null(await foreignStore.FindByInstanceAsync("definitively-unowned-instance"));
    }

    [Fact]
    public async Task WithdrawalAndNewEpochCannotReviveOldPreparation()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var subscription = (await store.FindSubscriptionAsync(prepared.SubscriptionId))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        Assert.Null(await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!));
        var reactivated = (await store.ActivateAsync(withdrawn.Id, withdrawn.Revision, AdmissionWorkerHost.Now))!;
        Assert.True(reactivated.ActivationEpoch > prepared.ActivationEpoch);
        Assert.Null(await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!));
        var current = (await store.FindAsync(prepared.Id))!;
        Assert.Equal(AdmissionState.StartPreparing, current.State);
        Assert.False(current.AuthorityOutstanding);
    }

    [Fact]
    public async Task TerminalClockReleasesActiveOnceAndOwnedTombstoneRemainsCharged()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, AdmissionWorkerHost.Configuration(1, 1));
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var authorized = (await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!))!;
        var terminalTime = AdmissionWorkerHost.Now.AddDays(10);
        var completed = (await store.CompleteExecutionAsync(authorized.Id, authorized.Revision, authorized.AttemptId!, AdmissionHash.Compute("finished"), [], true, terminalTime))!;
        Assert.Equal(terminalTime, completed.TerminalAt);
        Assert.Null(await store.CompleteExecutionAsync(authorized.Id, authorized.Revision, authorized.AttemptId!, AdmissionHash.Compute("finished"), [], true, terminalTime));
        Assert.False(await store.CleanupAsync(completed.Id, completed.Revision, "fixture-cleanup-authority", terminalTime));
        Assert.True(await store.CleanupAsync(completed.Id, completed.Revision, "fixture-cleanup-authority", terminalTime.AddDays(3)));
        var tombstone = (await store.FindByInstanceAsync(authorized.WorkflowInstanceId!))!;
        Assert.Null(tombstone.Payload);
        Assert.Null(tombstone.IdentityHash);
        Assert.False(tombstone.RetainedRecordReleased);
        var subscription = (await store.FindSubscriptionAsync(completed.SubscriptionId))!;
        Assert.Equal(0, subscription.ActiveReservations);
        Assert.Equal(1, subscription.RetainedRecords);
        var fresh = AdmissionWorkerHost.Event("fresh-after-owned-cleanup") with { OccurredAt = terminalTime.AddDays(3) };
        Assert.Equal(AdmissionOutcome.CapacityExceeded, (await store.AdmitAsync(fresh, terminalTime.AddDays(3))).Outcome);
    }

    [Fact]
    public async Task SuppressedUnallocatedCleanupReleasesRetainedCapacityOnlyOnce()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, AdmissionWorkerHost.Configuration(1, 1));
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:fixture-suppression", false, true, AdmissionWorkerHost.Now))!;
        Assert.True(await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3)));
        Assert.False(await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3)));
        Assert.Null(await store.FindAsync(terminal.Id));
        var subscription = (await store.FindSubscriptionAsync(terminal.SubscriptionId))!;
        Assert.Equal(0, subscription.ActiveReservations);
        Assert.Equal(0, subscription.RetainedRecords);
        Assert.Equal(AdmissionOutcome.Quarantined, (await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now.AddDays(3))).Outcome);
    }

    [Fact]
    public async Task RecoveryAndIssuedAuthorityCannotBeSuppressedOrResolvedWithoutBothAttestations()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var authorized = (await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!))!;
        var recovery = (await store.RequireRecoveryAsync(authorized.Id, authorized.Revision, "unknown:final-write"))!;
        Assert.True(recovery.AuthorityOutstanding);
        Assert.Null(await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.SuppressedBeforeStart, "audit:suppressed", true, true, AdmissionWorkerHost.Now));
        Assert.Null(await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:missing-quiescence", false, true, AdmissionWorkerHost.Now));
        Assert.Null(await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:unknown-effects", true, false, AdmissionWorkerHost.Now));
        Assert.Null(await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Completed, "audit:not-completed", true, true, AdmissionWorkerHost.Now));
        Assert.False(await store.CleanupAsync(recovery.Id, recovery.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddYears(10)));
        var resolved = (await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:verified-quiescent-effects", true, true, AdmissionWorkerHost.Now))!;
        Assert.Equal("audit:verified-quiescent-effects", resolved.AuditReference);
        Assert.Equal(AdmissionState.Terminal, resolved.State);
    }

    [Fact]
    public async Task CleanupUsesOriginalPolicyAndDuplicatesCompareDigestAfterPayloadErasure()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:fixture-suppression", false, true, AdmissionWorkerHost.Now))!;
        Assert.True(await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddHours(2)));
        Assert.Equal(AdmissionOutcome.Duplicate, (await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now.AddHours(2))).Outcome);
        Assert.Equal(AdmissionOutcome.Quarantined, (await store.AdmitAsync(AdmissionWorkerHost.Event() with { Payload = "conflicting-content" }, AdmissionWorkerHost.Now.AddHours(2))).Outcome);
        var subscription = (await store.FindSubscriptionAsync(terminal.SubscriptionId))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var configuration = AdmissionWorkerHost.Configuration() with { Policy = AdmissionWorkerHost.Configuration().Policy with { IdentityHorizon = TimeSpan.FromDays(30), CleanupAuthority = "new-cleanup-authority" } };
        await store.ReconfigureAsync(configuration, withdrawn.Revision);
        var current = (await store.FindAsync(terminal.Id))!;
        Assert.False(await store.CleanupAsync(current.Id, current.Revision, "new-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3)));
        Assert.True(await store.CleanupAsync(current.Id, current.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3)));
    }

    [Fact]
    public async Task SuspendedCheckpointLineageCanBeConsumedOnlyOnce()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var authorized = (await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!))!;
        var checkpoint = AdmissionHash.Compute("trusted-state-and-bookmarks");
        var suspended = (await store.CompleteExecutionAsync(authorized.Id, authorized.Revision, authorized.AttemptId!, checkpoint, ["bookmark-1"], false, AdmissionWorkerHost.Now))!;
        Assert.Null(suspended.TerminalAt);
        Assert.False(await store.CleanupAsync(suspended.Id, suspended.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddYears(10)));
        Assert.Null(await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-forged", AdmissionHash.Compute("forged-state"), "bookmark-1"));
        var continuation = (await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-2", checkpoint, "bookmark-1"))!;
        var consumed = (await store.AuthorizeStartAsync(continuation.Id, continuation.Revision, continuation.AttemptId!))!;
        Assert.Null(consumed.CheckpointFingerprint);
        Assert.Null(consumed.BookmarkIdsJson);
        Assert.Null(await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-replayed", checkpoint, "bookmark-1"));
    }

    [Theory]
    [InlineData("missing", AdmissionOutcome.Rejected)]
    [InlineData("future", AdmissionOutcome.Rejected)]
    [InlineData("skew-boundary", AdmissionOutcome.Committed)]
    [InlineData("late", AdmissionOutcome.Quarantined)]
    [InlineData("activation", AdmissionOutcome.Quarantined)]
    [InlineData("loop", AdmissionOutcome.Filtered)]
    [InlineData("nonhuman", AdmissionOutcome.Filtered)]
    [InlineData("channel", AdmissionOutcome.Quarantined)]
    [InlineData("utf8-payload", AdmissionOutcome.Rejected)]
    [InlineData("utf8-event-id", AdmissionOutcome.Rejected)]
    public async Task ExplicitEventPolicyRejectsInvalidOrLateInput(string parameterId, AdmissionOutcome expected)
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var message = AdmissionWorkerHost.Event();
        message = parameterId switch
        {
            "missing" => message with { OccurredAt = null },
            "future" => message with { OccurredAt = AdmissionWorkerHost.Now.AddMinutes(2) },
            "skew-boundary" => message with { OccurredAt = AdmissionWorkerHost.Now.AddMinutes(1) },
            "late" => message with { OccurredAt = AdmissionWorkerHost.Now.AddDays(-2) },
            "activation" => message with { OccurredAt = AdmissionWorkerHost.Now.AddMinutes(-2) },
            "loop" => message with { IsLoopMessage = true },
            "nonhuman" => message with { IsHumanMessage = false },
            "channel" => message with { ChannelId = "payload-selected-channel" },
            "utf8-payload" => message with { Payload = new string('€', 1500) },
            "utf8-event-id" => message with { ProviderEventId = new string('€', 100) },
            _ => throw new ArgumentOutOfRangeException(nameof(parameterId))
        };
        Assert.Equal(expected, (await AdmissionTestLedger.Store(services).AdmitAsync(message, AdmissionWorkerHost.Now)).Outcome);
    }

    [Fact]
    public async Task ExplicitEventIdByteBoundaryMatchesPostgreSqlStorage()
    {
        var configuration = AdmissionWorkerHost.Configuration() with
        {
            Policy = AdmissionWorkerHost.Configuration().Policy with { MaximumProviderEventIdBytes = AdmissionLimits.ProviderEventIdBytes }
        };
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, configuration);
        var store = AdmissionTestLedger.Store(services);
        var message = AdmissionWorkerHost.Event(new string('e', AdmissionLimits.ProviderEventIdBytes));
        var admitted = await store.AdmitAsync(message, AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        Assert.Equal(message.ProviderEventId, (await store.FindAsync(admitted.AdmissionId!))!.ProviderEventId);
        Assert.Equal(AdmissionOutcome.Rejected, (await store.AdmitAsync(message with { ProviderEventId = message.ProviderEventId + "e" }, AdmissionWorkerHost.Now)).Outcome);
    }

    [Fact]
    public async Task RecoveryCursorReachesLaterAdmissionsBeyondUnresolvedPages()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        for (var index = 0; index < 3; index++)
        {
            await store.AdmitAsync(AdmissionWorkerHost.Event("cursor-event-" + index), AdmissionWorkerHost.Now);
        }
        var ordered = await store.FindRecoverableAsync(3, null);
        foreach (var record in ordered.Take(2))
        {
            Assert.NotNull(await store.RequireRecoveryAsync(record.Id, record.Revision, "unknown:retained-owner"));
        }
        var visited = new List<AdmissionRecord>();
        string? afterId = null;
        while (true)
        {
            var page = await store.FindRecoverableAsync(1, afterId);
            if (page.Count == 0)
            {
                break;
            }
            Assert.Single(page);
            visited.Add(page[0]);
            afterId = page[0].Id;
        }
        Assert.Equal(ordered.Select(x => x.Id), visited.Select(x => x.Id));
        Assert.Equal(AdmissionState.Admitted, visited[2].State);
        Assert.All(visited.Take(2), record => Assert.Equal(AdmissionState.RecoveryRequired, record.State));
    }

    [Fact]
    public async Task RetiredNamespaceCannotBeResetAndRecoveryEnumerationIsBounded()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        await store.AdmitAsync(AdmissionWorkerHost.Event("event-a"), AdmissionWorkerHost.Now);
        await store.AdmitAsync(AdmissionWorkerHost.Event("event-b"), AdmissionWorkerHost.Now);
        Assert.Single(await store.FindRecoverableAsync(1, null));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.FindRecoverableAsync(0, null));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.FindRecoverableAsync(1001, null));
        var subscription = (await store.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;
        var retired = (await store.WithdrawAsync(subscription.Id, subscription.Revision, true, null))!;
        Assert.Null(await store.ActivateAsync(retired.Id, retired.Revision, AdmissionWorkerHost.Now));
        await Assert.ThrowsAsync<InvalidOperationException>(() => store.ProvisionAsync(AdmissionWorkerHost.Configuration()));
        Assert.Equal(AdmissionOutcome.Inactive, (await store.AdmitAsync(AdmissionWorkerHost.Event("new-event"), AdmissionWorkerHost.Now)).Outcome);
    }
}

internal static class AdmissionTestLedger
{
    public static IAdmissionStore Store(IServiceProvider services) => services.GetRequiredService<IAdmissionStore>();
    public static Task<AdmissionElsaDbContext> ContextAsync(IServiceProvider services) => services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>().CreateDbContextAsync();

    public static async Task<ServiceProvider> CreateAsync(PostgreSqlConnectionsFixture fixture, AdmissionSubscriptionConfiguration? configuration = null)
    {
        await fixture.ResetSchemaAsync();
        var services = AdmissionWorkerHost.CreateServices(fixture.ConnectionString);
        try
        {
            await AdmissionWorkerHost.MigrateAsync(services);
            await ActivateAsync(Store(services), configuration ?? AdmissionWorkerHost.Configuration());
            return services;
        }
        catch
        {
            await services.DisposeAsync();
            throw;
        }
    }

    public static async Task ActivateAsync(IAdmissionStore store, AdmissionSubscriptionConfiguration configuration)
    {
        var provisioned = await store.ProvisionAsync(configuration);
        var verified = (await store.VerifyBootstrapAsync(provisioned.Id, provisioned.Revision, configuration.ConfigurationFingerprint))!;
        Assert.NotNull(await store.ActivateAsync(verified.Id, verified.Revision, AdmissionWorkerHost.Now));
    }

    public static async Task<AdmissionRecord> PrepareAsync(IAdmissionStore store)
    {
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var creating = (await store.BeginCreationAsync(admitted.AdmissionId!, admitted.Revision!.Value, "owned-instance-fixed"))!;
        var materialized = (await store.CompleteCreationAsync(creating.Id, creating.Revision, AdmissionHash.Compute("materialized-state")))!;
        return (await store.PrepareStartAsync(materialized.Id, materialized.Revision, "attempt-1", null, null))!;
    }
}
