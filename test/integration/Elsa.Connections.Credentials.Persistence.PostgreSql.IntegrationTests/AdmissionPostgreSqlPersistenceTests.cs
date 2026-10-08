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
        var migrations = db.Database.GetMigrations().ToArray();
        var pendingModelChanges = db.Database.HasPendingModelChanges();
        Assert.Single(migrations);
        Assert.False(pendingModelChanges);
        var admission = await AdmissionTestLedger.Store(services).AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        await db.Database.MigrateAsync();
        var reloaded = await AdmissionTestLedger.Store(services).FindAsync(admission.AdmissionId!);
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.NotNull(reloaded);
        Assert.Single(appliedMigrations);
        await ObserveAsync("provider-store-migration", nameof(MigrationMatchesRuntimeModelAndReapplicationPreservesLedger),
            migrations.Length == 1 && !pendingModelChanges && reloaded != null && appliedMigrations.Length == 1,
            new() { ["migrationCount"] = migrations.Length, ["appliedMigrationCount"] = appliedMigrations.Length,
                ["pendingModelChanges"] = pendingModelChanges, ["ledgerPresent"] = reloaded != null });
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
        var changed = await store.AdmitAsync(AdmissionWorkerHost.Event() with { Payload = "edited-same-provider-event" }, AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Quarantined, changed.Outcome);
        var fanout = AdmissionWorkerHost.Configuration(id: "independent-subscription");
        await AdmissionTestLedger.ActivateAsync(store, fanout);
        var second = await store.AdmitAsync(AdmissionWorkerHost.Event(subscriptionId: fanout.Id), AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Committed, second.Outcome);
        Assert.NotEqual(first.AdmissionId, second.AdmissionId);
        var subscription = (await store.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;
        Assert.Equal(1, subscription.ActiveReservations);
        await ObserveAsync("provider-store-identity-fanout", nameof(IdentityFanOutAndChangedDuplicatePreserveOriginalBinding),
            first.Outcome == AdmissionOutcome.Committed && duplicate.Outcome == AdmissionOutcome.Duplicate
            && first.AdmissionId == duplicate.AdmissionId && changed.Outcome == AdmissionOutcome.Quarantined
            && second.Outcome == AdmissionOutcome.Committed && first.AdmissionId != second.AdmissionId && subscription.ActiveReservations == 1,
            new() { ["firstOutcome"] = first.Outcome.ToString(), ["duplicateOutcome"] = duplicate.Outcome.ToString(),
                ["changedOutcome"] = changed.Outcome.ToString(), ["fanoutOutcome"] = second.Outcome.ToString(),
                ["sameAllocation"] = first.AdmissionId == duplicate.AdmissionId,
                ["independentAllocation"] = first.AdmissionId != second.AdmissionId, ["activeReservations"] = subscription.ActiveReservations });
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
        var instanceError = await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.FindByInstanceAsync("globally-owned-instance"));
        var subscriptionError = await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id));
        var provisionError = await Assert.ThrowsAsync<InvalidOperationException>(() => foreignStore.ProvisionAsync(AdmissionWorkerHost.Configuration()));
        var unowned = await foreignStore.FindByInstanceAsync("definitively-unowned-instance");
        Assert.Null(unowned);
        await ObserveAsync("provider-store-foreign-scope", nameof(ForeignScopeOwnershipNeverAppearsUnowned),
            instanceError != null && subscriptionError != null && provisionError != null && unowned == null,
            new() { ["foreignInstanceDenied"] = instanceError != null, ["foreignSubscriptionDenied"] = subscriptionError != null,
                ["foreignProvisionDenied"] = provisionError != null, ["unownedInstanceAbsent"] = unowned == null });
    }

    [Fact]
    public async Task WithdrawalAndNewEpochCannotReviveOldPreparation()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var prepared = await AdmissionTestLedger.PrepareAsync(store);
        var subscription = (await store.FindSubscriptionAsync(prepared.SubscriptionId))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var afterWithdrawal = await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!);
        Assert.Null(afterWithdrawal);
        var reactivated = (await store.ActivateAsync(withdrawn.Id, withdrawn.Revision, AdmissionWorkerHost.Now))!;
        Assert.True(reactivated.ActivationEpoch > prepared.ActivationEpoch);
        var afterReactivation = await store.AuthorizeStartAsync(prepared.Id, prepared.Revision, prepared.AttemptId!);
        Assert.Null(afterReactivation);
        var current = (await store.FindAsync(prepared.Id))!;
        Assert.Equal(AdmissionState.StartPreparing, current.State);
        Assert.False(current.AuthorityOutstanding);
        await ObserveAsync("provider-store-epoch-withdrawal", nameof(WithdrawalAndNewEpochCannotReviveOldPreparation),
            afterWithdrawal == null && afterReactivation == null && reactivated.ActivationEpoch > prepared.ActivationEpoch
            && current.State == AdmissionState.StartPreparing && !current.AuthorityOutstanding,
            new() { ["withdrawalDenied"] = afterWithdrawal == null, ["reactivationDenied"] = afterReactivation == null,
                ["epochAdvanced"] = reactivated.ActivationEpoch > prepared.ActivationEpoch,
                ["state"] = current.State.ToString(), ["authorityOutstanding"] = current.AuthorityOutstanding });
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
        var repeatedCompletion = await store.CompleteExecutionAsync(authorized.Id, authorized.Revision, authorized.AttemptId!, AdmissionHash.Compute("finished"), [], true, terminalTime);
        Assert.Null(repeatedCompletion);
        var earlyCleanup = await store.CleanupAsync(completed.Id, completed.Revision, "fixture-cleanup-authority", terminalTime);
        Assert.False(earlyCleanup);
        var matureCleanup = await store.CleanupAsync(completed.Id, completed.Revision, "fixture-cleanup-authority", terminalTime.AddDays(3));
        Assert.True(matureCleanup);
        var tombstone = (await store.FindByInstanceAsync(authorized.WorkflowInstanceId!))!;
        Assert.Null(tombstone.Payload);
        Assert.Null(tombstone.IdentityHash);
        Assert.False(tombstone.RetainedRecordReleased);
        var subscription = (await store.FindSubscriptionAsync(completed.SubscriptionId))!;
        Assert.Equal(0, subscription.ActiveReservations);
        Assert.Equal(1, subscription.RetainedRecords);
        var fresh = AdmissionWorkerHost.Event("fresh-after-owned-cleanup") with { OccurredAt = terminalTime.AddDays(3) };
        var capacity = await store.AdmitAsync(fresh, terminalTime.AddDays(3));
        Assert.Equal(AdmissionOutcome.CapacityExceeded, capacity.Outcome);
        await ObserveAsync("provider-store-terminal-clock", nameof(TerminalClockReleasesActiveOnceAndOwnedTombstoneRemainsCharged),
            completed.TerminalAt == terminalTime && repeatedCompletion == null && !earlyCleanup && matureCleanup
            && tombstone.Payload == null && tombstone.IdentityHash == null && !tombstone.RetainedRecordReleased
            && subscription.ActiveReservations == 0 && subscription.RetainedRecords == 1 && capacity.Outcome == AdmissionOutcome.CapacityExceeded,
            new() { ["terminalClockMatches"] = completed.TerminalAt == terminalTime, ["repeatedCompletionDenied"] = repeatedCompletion == null,
                ["earlyCleanupAllowed"] = earlyCleanup, ["matureCleanupAllowed"] = matureCleanup,
                ["payloadErased"] = tombstone.Payload == null, ["identityErased"] = tombstone.IdentityHash == null,
                ["retainedRecordReleased"] = tombstone.RetainedRecordReleased, ["activeReservations"] = subscription.ActiveReservations,
                ["retainedRecords"] = subscription.RetainedRecords, ["nextOutcome"] = capacity.Outcome.ToString() });
    }

    [Fact]
    public async Task SuppressedUnallocatedCleanupReleasesRetainedCapacityOnlyOnce()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, AdmissionWorkerHost.Configuration(1, 1));
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:fixture-suppression", false, true, AdmissionWorkerHost.Now))!;
        var firstCleanup = await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3));
        Assert.True(firstCleanup);
        var secondCleanup = await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3));
        Assert.False(secondCleanup);
        var remaining = await store.FindAsync(terminal.Id);
        Assert.Null(remaining);
        var subscription = (await store.FindSubscriptionAsync(terminal.SubscriptionId))!;
        Assert.Equal(0, subscription.ActiveReservations);
        Assert.Equal(0, subscription.RetainedRecords);
        var late = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now.AddDays(3));
        Assert.Equal(AdmissionOutcome.Quarantined, late.Outcome);
        await ObserveAsync("provider-store-suppressed-cleanup", nameof(SuppressedUnallocatedCleanupReleasesRetainedCapacityOnlyOnce),
            firstCleanup && !secondCleanup && remaining == null && subscription.ActiveReservations == 0
            && subscription.RetainedRecords == 0 && late.Outcome == AdmissionOutcome.Quarantined,
            new() { ["firstCleanupAllowed"] = firstCleanup, ["secondCleanupAllowed"] = secondCleanup,
                ["recordAbsent"] = remaining == null, ["activeReservations"] = subscription.ActiveReservations,
                ["retainedRecords"] = subscription.RetainedRecords, ["lateOutcome"] = late.Outcome.ToString() });
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
        var suppression = await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.SuppressedBeforeStart, "audit:suppressed", true, true, AdmissionWorkerHost.Now);
        Assert.Null(suppression);
        var withoutQuiescence = await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:missing-quiescence", false, true, AdmissionWorkerHost.Now);
        Assert.Null(withoutQuiescence);
        var withoutEffects = await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:unknown-effects", true, false, AdmissionWorkerHost.Now);
        Assert.Null(withoutEffects);
        var completion = await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Completed, "audit:not-completed", true, true, AdmissionWorkerHost.Now);
        Assert.Null(completion);
        var cleanup = await store.CleanupAsync(recovery.Id, recovery.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddYears(10));
        Assert.False(cleanup);
        var resolved = (await store.ResolveAsync(recovery.Id, recovery.Revision, AdmissionTerminalDisposition.Resolved, "audit:verified-quiescent-effects", true, true, AdmissionWorkerHost.Now))!;
        Assert.Equal("audit:verified-quiescent-effects", resolved.AuditReference);
        Assert.Equal(AdmissionState.Terminal, resolved.State);
        await ObserveAsync("provider-store-recovery-attestations", nameof(RecoveryAndIssuedAuthorityCannotBeSuppressedOrResolvedWithoutBothAttestations),
            recovery.AuthorityOutstanding && suppression == null && withoutQuiescence == null && withoutEffects == null
            && completion == null && !cleanup && resolved.AuditReference == "audit:verified-quiescent-effects" && resolved.State == AdmissionState.Terminal,
            new() { ["authorityOutstanding"] = recovery.AuthorityOutstanding, ["suppressionDenied"] = suppression == null,
                ["missingQuiescenceDenied"] = withoutQuiescence == null, ["missingEffectsDenied"] = withoutEffects == null,
                ["completionDenied"] = completion == null, ["cleanupAllowed"] = cleanup,
                ["auditReferenceMatches"] = resolved.AuditReference == "audit:verified-quiescent-effects", ["state"] = resolved.State.ToString() });
    }

    [Fact]
    public async Task CleanupUsesOriginalPolicyAndDuplicatesCompareDigestAfterPayloadErasure()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        var admitted = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:fixture-suppression", false, true, AdmissionWorkerHost.Now))!;
        var payloadCleanup = await store.CleanupAsync(terminal.Id, terminal.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddHours(2));
        Assert.True(payloadCleanup);
        var duplicate = await store.AdmitAsync(AdmissionWorkerHost.Event(), AdmissionWorkerHost.Now.AddHours(2));
        Assert.Equal(AdmissionOutcome.Duplicate, duplicate.Outcome);
        var conflict = await store.AdmitAsync(AdmissionWorkerHost.Event() with { Payload = "conflicting-content" }, AdmissionWorkerHost.Now.AddHours(2));
        Assert.Equal(AdmissionOutcome.Quarantined, conflict.Outcome);
        var subscription = (await store.FindSubscriptionAsync(terminal.SubscriptionId))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var configuration = AdmissionWorkerHost.Configuration() with { Policy = AdmissionWorkerHost.Configuration().Policy with { IdentityHorizon = TimeSpan.FromDays(30), CleanupAuthority = "new-cleanup-authority" } };
        await store.ReconfigureAsync(configuration, withdrawn.Revision);
        var current = (await store.FindAsync(terminal.Id))!;
        var newAuthorityCleanup = await store.CleanupAsync(current.Id, current.Revision, "new-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3));
        Assert.False(newAuthorityCleanup);
        var originalAuthorityCleanup = await store.CleanupAsync(current.Id, current.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddDays(3));
        Assert.True(originalAuthorityCleanup);
        await ObserveAsync("provider-store-original-cleanup-policy", nameof(CleanupUsesOriginalPolicyAndDuplicatesCompareDigestAfterPayloadErasure),
            payloadCleanup && duplicate.Outcome == AdmissionOutcome.Duplicate && conflict.Outcome == AdmissionOutcome.Quarantined
            && !newAuthorityCleanup && originalAuthorityCleanup,
            new() { ["payloadCleanupAllowed"] = payloadCleanup, ["duplicateOutcome"] = duplicate.Outcome.ToString(),
                ["conflictOutcome"] = conflict.Outcome.ToString(), ["newAuthorityCleanupAllowed"] = newAuthorityCleanup,
                ["originalAuthorityCleanupAllowed"] = originalAuthorityCleanup });
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
        var cleanup = await store.CleanupAsync(suspended.Id, suspended.Revision, "fixture-cleanup-authority", AdmissionWorkerHost.Now.AddYears(10));
        Assert.False(cleanup);
        var forged = await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-forged", AdmissionHash.Compute("forged-state"), "bookmark-1");
        Assert.Null(forged);
        var continuation = (await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-2", checkpoint, "bookmark-1"))!;
        var consumed = (await store.AuthorizeStartAsync(continuation.Id, continuation.Revision, continuation.AttemptId!))!;
        Assert.Null(consumed.CheckpointFingerprint);
        Assert.Null(consumed.BookmarkIdsJson);
        var replayed = await store.PrepareStartAsync(suspended.Id, suspended.Revision, "attempt-replayed", checkpoint, "bookmark-1");
        Assert.Null(replayed);
        await ObserveAsync("provider-store-continuation-lineage", nameof(SuspendedCheckpointLineageCanBeConsumedOnlyOnce),
            suspended.TerminalAt == null && !cleanup && forged == null && consumed.CheckpointFingerprint == null
            && consumed.BookmarkIdsJson == null && replayed == null,
            new() { ["terminalClockAbsent"] = suspended.TerminalAt == null, ["cleanupAllowed"] = cleanup,
                ["forgedCheckpointDenied"] = forged == null, ["checkpointConsumed"] = consumed.CheckpointFingerprint == null,
                ["bookmarkLineageConsumed"] = consumed.BookmarkIdsJson == null, ["replayDenied"] = replayed == null });
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
        var result = await AdmissionTestLedger.Store(services).AdmitAsync(message, AdmissionWorkerHost.Now);
        Assert.Equal(expected, result.Outcome);
        await ObserveAsync("provider-store-policy-" + parameterId, nameof(ExplicitEventPolicyRejectsInvalidOrLateInput),
            result.Outcome == expected, new() { ["outcome"] = result.Outcome.ToString(),
                ["acknowledgementEligible"] = result.AcknowledgementEligible }, parameterId);
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
        var stored = (await store.FindAsync(admitted.AdmissionId!))!;
        Assert.Equal(message.ProviderEventId, stored.ProviderEventId);
        var oversized = await store.AdmitAsync(message with { ProviderEventId = message.ProviderEventId + "e" }, AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Rejected, oversized.Outcome);
        await ObserveAsync("provider-store-event-id-boundary", nameof(ExplicitEventIdByteBoundaryMatchesPostgreSqlStorage),
            admitted.Outcome == AdmissionOutcome.Committed && message.ProviderEventId == stored.ProviderEventId && oversized.Outcome == AdmissionOutcome.Rejected,
            new() { ["boundaryOutcome"] = admitted.Outcome.ToString(), ["eventIdPreserved"] = message.ProviderEventId == stored.ProviderEventId,
                ["oversizedOutcome"] = oversized.Outcome.ToString() });
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
        await ObserveAsync("provider-store-recovery-cursor", nameof(RecoveryCursorReachesLaterAdmissionsBeyondUnresolvedPages),
            ordered.Select(x => x.Id).SequenceEqual(visited.Select(x => x.Id)) && visited[2].State == AdmissionState.Admitted
            && visited.Take(2).All(record => record.State == AdmissionState.RecoveryRequired),
            new() { ["visitedCount"] = visited.Count, ["expectedCount"] = ordered.Count,
                ["orderPreserved"] = ordered.Select(x => x.Id).SequenceEqual(visited.Select(x => x.Id)),
                ["recoveryRequiredCount"] = visited.Count(record => record.State == AdmissionState.RecoveryRequired),
                ["laterState"] = visited[2].State.ToString() });
    }

    [Fact]
    public async Task RetiredNamespaceCannotBeResetAndRecoveryEnumerationIsBounded()
    {
        await using var services = await AdmissionTestLedger.CreateAsync(fixture);
        var store = AdmissionTestLedger.Store(services);
        await store.AdmitAsync(AdmissionWorkerHost.Event("event-a"), AdmissionWorkerHost.Now);
        await store.AdmitAsync(AdmissionWorkerHost.Event("event-b"), AdmissionWorkerHost.Now);
        var page = await store.FindRecoverableAsync(1, null);
        Assert.Single(page);
        var zeroLimit = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.FindRecoverableAsync(0, null));
        var excessiveLimit = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => store.FindRecoverableAsync(1001, null));
        var subscription = (await store.FindSubscriptionAsync(AdmissionWorkerHost.Configuration().Id))!;
        var retired = (await store.WithdrawAsync(subscription.Id, subscription.Revision, true, null))!;
        var reactivation = await store.ActivateAsync(retired.Id, retired.Revision, AdmissionWorkerHost.Now);
        Assert.Null(reactivation);
        var reprovision = await Assert.ThrowsAsync<InvalidOperationException>(() => store.ProvisionAsync(AdmissionWorkerHost.Configuration()));
        var inactive = await store.AdmitAsync(AdmissionWorkerHost.Event("new-event"), AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Inactive, inactive.Outcome);
        await ObserveAsync("provider-store-retired-namespace", nameof(RetiredNamespaceCannotBeResetAndRecoveryEnumerationIsBounded),
            page.Count == 1 && zeroLimit != null && excessiveLimit != null && reactivation == null && reprovision != null && inactive.Outcome == AdmissionOutcome.Inactive,
            new() { ["pageCount"] = page.Count, ["zeroLimitDenied"] = zeroLimit != null,
                ["excessiveLimitDenied"] = excessiveLimit != null, ["reactivationDenied"] = reactivation == null,
                ["reprovisionDenied"] = reprovision != null, ["outcome"] = inactive.Outcome.ToString() });
    }

    [Fact]
    public async Task PostgreSqlTimestampPrecisionAndClockRollbackCannotShortenIdentityRetention()
    {
        var now = AdmissionWorkerHost.Now.AddTicks(5);
        var maximumAge = TimeSpan.FromDays(1);
        var clockSkew = TimeSpan.FromMinutes(1);
        var horizon = maximumAge + clockSkew + TimeSpan.FromTicks(1);
        var configuration = AdmissionWorkerHost.Configuration() with
        {
            ActivationBoundary = now.AddMinutes(-1),
            Policy = AdmissionWorkerHost.Configuration().Policy with
            {
                MaximumEventAge = maximumAge, MaximumClockSkew = clockSkew, IdentityHorizon = horizon
            }
        };
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, configuration);
        var store = AdmissionTestLedger.Store(services);
        var message = AdmissionWorkerHost.Event("submicrosecond-event") with { OccurredAt = now + clockSkew };
        var admitted = await store.AdmitAsync(message, now);
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:precision-suppression", false, true, now))!;
        var persisted = (await store.FindAsync(terminal.Id))!;
        Assert.True(persisted.AdmittedAt >= now);
        Assert.True(persisted.TerminalAt >= persisted.AdmittedAt);
        Assert.Equal(0, persisted.AdmittedAt.UtcDateTime.Ticks % 10);
        Assert.Equal(0, persisted.TerminalAt!.Value.UtcDateTime.Ticks % 10);
        var cleanupTime = AdmissionWorkerHost.Now + horizon;
        Assert.True(cleanupTime < message.OccurredAt!.Value + maximumAge);
        var cleanup = await store.CleanupAsync(persisted.Id, persisted.Revision, configuration.Policy.CleanupAuthority, cleanupTime);
        Assert.True(cleanup);
        var retained = await store.FindAsync(persisted.Id);
        Assert.NotNull(retained);
        Assert.NotNull(retained.IdentityHash);
        var redelivery = await store.AdmitAsync(message, cleanupTime);
        Assert.Equal(AdmissionOutcome.Duplicate, redelivery.Outcome);
        Assert.Equal(admitted.AdmissionId, redelivery.AdmissionId);
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        var boundaryCount = await db.Admissions.CountAsync();
        Assert.Equal(1, boundaryCount);

        var rollbackAdmission = await store.AdmitAsync(AdmissionWorkerHost.Event("rollback-terminal-clock") with { OccurredAt = now }, now);
        Assert.Equal(AdmissionOutcome.Committed, rollbackAdmission.Outcome);
        var rollbackTerminal = (await store.ResolveAsync(rollbackAdmission.AdmissionId!, rollbackAdmission.Revision!.Value,
            AdmissionTerminalDisposition.SuppressedBeforeStart, "audit:rollback-suppression", false, true, now.AddDays(-3)))!;
        var rollbackPersisted = (await store.FindAsync(rollbackTerminal.Id))!;
        Assert.True(rollbackPersisted.TerminalAt >= rollbackPersisted.AdmittedAt);
        Assert.True(rollbackPersisted.AdmittedAt >= now);
        var immediateCleanup = await store.CleanupAsync(rollbackPersisted.Id, rollbackPersisted.Revision, configuration.Policy.CleanupAuthority, now);
        Assert.False(immediateCleanup);
        var rollbackRetained = await store.FindAsync(rollbackPersisted.Id);
        Assert.NotNull(rollbackRetained);
        Assert.NotNull(rollbackRetained.IdentityHash);
        await ObserveAsync("provider-store-microsecond-retention", nameof(PostgreSqlTimestampPrecisionAndClockRollbackCannotShortenIdentityRetention),
            persisted.AdmittedAt >= now && persisted.TerminalAt >= persisted.AdmittedAt
            && persisted.AdmittedAt.UtcDateTime.Ticks % 10 == 0 && persisted.TerminalAt!.Value.UtcDateTime.Ticks % 10 == 0
            && cleanupTime < message.OccurredAt!.Value + maximumAge && cleanup && retained.IdentityHash != null
            && redelivery.Outcome == AdmissionOutcome.Duplicate && admitted.AdmissionId == redelivery.AdmissionId && boundaryCount == 1
            && rollbackPersisted.TerminalAt >= rollbackPersisted.AdmittedAt && rollbackPersisted.AdmittedAt >= now
            && !immediateCleanup && rollbackRetained.IdentityHash != null,
            new() { ["admissionClockNotRoundedEarlier"] = persisted.AdmittedAt >= now,
                ["terminalClockNotBeforeAdmission"] = persisted.TerminalAt >= persisted.AdmittedAt,
                ["admissionMicrosecondAligned"] = persisted.AdmittedAt.UtcDateTime.Ticks % 10 == 0,
                ["terminalMicrosecondAligned"] = persisted.TerminalAt!.Value.UtcDateTime.Ticks % 10 == 0,
                ["redeliveryWithinOriginalAgeWindow"] = cleanupTime < message.OccurredAt!.Value + maximumAge,
                ["boundaryCleanupAllowed"] = cleanup,
                ["identityRetainedAtBoundary"] = retained.IdentityHash != null, ["redeliveryOutcome"] = redelivery.Outcome.ToString(),
                ["sameAllocation"] = admitted.AdmissionId == redelivery.AdmissionId, ["boundaryAdmissionCount"] = boundaryCount,
                ["rollbackTerminalNotBeforeAdmission"] = rollbackPersisted.TerminalAt >= rollbackPersisted.AdmittedAt,
                ["rollbackAdmissionClockNotRoundedEarlier"] = rollbackPersisted.AdmittedAt >= now,
                ["immediateCleanupAllowed"] = immediateCleanup, ["rollbackIdentityRetained"] = rollbackRetained.IdentityHash != null });
    }

    [Fact]
    public async Task InactiveReconfigurationCannotExpandEventAgeBeforeOrAfterIdentityPurge()
    {
        var configuration = AdmissionWorkerHost.Configuration();
        await using var services = await AdmissionTestLedger.CreateAsync(fixture, configuration);
        var store = AdmissionTestLedger.Store(services);
        var message = AdmissionWorkerHost.Event("old-window-event");
        var admitted = await store.AdmitAsync(message, AdmissionWorkerHost.Now);
        Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
        var terminal = (await store.ResolveAsync(admitted.AdmissionId!, admitted.Revision!.Value, AdmissionTerminalDisposition.SuppressedBeforeStart,
            "audit:window-suppression", false, true, AdmissionWorkerHost.Now))!;
        var subscription = (await store.FindSubscriptionAsync(configuration.Id))!;
        var withdrawn = (await store.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var widened = configuration with
        {
            Policy = configuration.Policy with { MaximumEventAge = TimeSpan.FromDays(30), IdentityHorizon = TimeSpan.FromDays(31) }
        };
        var beforePurge = await store.ReconfigureAsync(widened, withdrawn.Revision);
        Assert.Null(beforePurge);
        var unchanged = (await store.FindSubscriptionAsync(configuration.Id))!;
        Assert.Equal(withdrawn.ConfigurationJson, unchanged.ConfigurationJson);
        Assert.Equal(withdrawn.ConfigurationFingerprint, unchanged.ConfigurationFingerprint);
        Assert.Equal(withdrawn.Revision, unchanged.Revision);
        Assert.Equal(withdrawn.ActiveReservations, unchanged.ActiveReservations);
        Assert.Equal(withdrawn.RetainedRecords, unchanged.RetainedRecords);
        var cleanupTime = AdmissionWorkerHost.Now.AddDays(3);
        Assert.True(await store.CleanupAsync(terminal.Id, terminal.Revision, configuration.Policy.CleanupAuthority, cleanupTime));
        var purged = await store.FindAsync(terminal.Id);
        Assert.Null(purged);
        var afterCleanup = (await store.FindSubscriptionAsync(configuration.Id))!;
        Assert.Equal(0, afterCleanup.ActiveReservations);
        Assert.Equal(0, afterCleanup.RetainedRecords);
        var afterPurge = await store.ReconfigureAsync(widened, afterCleanup.Revision);
        Assert.Null(afterPurge);
        var stillUnchanged = (await store.FindSubscriptionAsync(configuration.Id))!;
        Assert.Equal(afterCleanup.ConfigurationJson, stillUnchanged.ConfigurationJson);
        Assert.Equal(afterCleanup.ConfigurationFingerprint, stillUnchanged.ConfigurationFingerprint);
        Assert.Equal(afterCleanup.Revision, stillUnchanged.Revision);
        Assert.Equal(afterCleanup.ActiveReservations, stillUnchanged.ActiveReservations);
        Assert.Equal(afterCleanup.RetainedRecords, stillUnchanged.RetainedRecords);
        Assert.NotNull(await store.ActivateAsync(stillUnchanged.Id, stillUnchanged.Revision, cleanupTime));
        var redelivery = await store.AdmitAsync(message, cleanupTime);
        Assert.Equal(AdmissionOutcome.Quarantined, redelivery.Outcome);
        Assert.False(redelivery.AcknowledgementEligible);
        await using var db = await AdmissionTestLedger.ContextAsync(services);
        var count = await db.Admissions.CountAsync();
        Assert.Equal(0, count);
        await ObserveAsync("provider-store-nonexpanding-event-window", nameof(InactiveReconfigurationCannotExpandEventAgeBeforeOrAfterIdentityPurge),
            beforePurge == null && afterPurge == null && unchanged.ConfigurationJson == withdrawn.ConfigurationJson
            && unchanged.ConfigurationFingerprint == withdrawn.ConfigurationFingerprint
            && unchanged.Revision == withdrawn.Revision && unchanged.ActiveReservations == withdrawn.ActiveReservations
            && unchanged.RetainedRecords == withdrawn.RetainedRecords && stillUnchanged.ConfigurationJson == afterCleanup.ConfigurationJson
            && stillUnchanged.ConfigurationFingerprint == afterCleanup.ConfigurationFingerprint
            && stillUnchanged.Revision == afterCleanup.Revision && stillUnchanged.ActiveReservations == 0 && stillUnchanged.RetainedRecords == 0
            && purged == null && redelivery.Outcome == AdmissionOutcome.Quarantined && !redelivery.AcknowledgementEligible && count == 0,
            new() { ["wideningDeniedBeforePurge"] = beforePurge == null, ["wideningDeniedAfterPurge"] = afterPurge == null,
                ["configurationPreservedBeforePurge"] = unchanged.ConfigurationJson == withdrawn.ConfigurationJson,
                ["configurationPreservedAfterPurge"] = stillUnchanged.ConfigurationJson == afterCleanup.ConfigurationJson,
                ["revisionPreservedBeforePurge"] = unchanged.Revision == withdrawn.Revision,
                ["revisionPreservedAfterPurge"] = stillUnchanged.Revision == afterCleanup.Revision,
                ["activeReservations"] = stillUnchanged.ActiveReservations, ["retainedRecords"] = stillUnchanged.RetainedRecords,
                ["oldIdentityPurged"] = purged == null, ["redeliveryOutcome"] = redelivery.Outcome.ToString(),
                ["acknowledgementEligible"] = redelivery.AcknowledgementEligible, ["admissionCount"] = count });
    }

    private Task ObserveAsync(string caseId, string method, bool verified, Dictionary<string, object> facts, string parameterId = "default") =>
        AdmissionProofObservation.WriteAsync(fixture, caseId, GetType().FullName + "." + method, parameterId, [],
            new Dictionary<string, bool> { ["durablePredicatesVerified"] = verified }, facts);
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
