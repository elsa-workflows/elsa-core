using Elsa.Slack.SocketMode.Persistence;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.Persistence.EFCore;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class SocketDiscardPersistenceTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task ReceiptMigrationIsSeparateAndReapplicationPreservesBothKinds()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var workflow = await host.Admissions.AdmitAsync(AdmissionWorkerHost.Event("workflow-first"), SocketDiscardTestFixture.Now);
        var request = await host.RequestAsync();
        var discard = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        var before = await host.SubscriptionAsync();
        await using var admission = await host.AdmissionContextAsync();
        await using var receipts = await host.ReceiptContextAsync();
        Assert.False(receipts.Database.HasPendingModelChanges());
        Assert.Equal(new[] { "20261008110000_InitialSlackSocketReceipts" }, receipts.Database.GetMigrations());
        Assert.Equal(new[] { "20261008040000_InitialAdmission" }, admission.Database.GetMigrations());
        await receipts.Database.MigrateAsync();
        await admission.Database.MigrateAsync();
        await host.Discards.ValidateProvisioningAsync();
        Assert.NotNull(await host.Admissions.FindAsync(workflow.AdmissionId!));
        Assert.NotNull(await host.ReceiptAsync(discard.ReceiptId!));
        var after = await host.SubscriptionAsync();
        Assert.Equal(before.RetainedRecords, after.RetainedRecords);
        Assert.Equal(before.ActiveReservations, after.ActiveReservations);
        Assert.Equal(before.Revision, after.Revision);
        var receiptModel = receipts.Model.FindEntityType(typeof(SlackSocketDiscardReceipt))!;
        Assert.DoesNotContain(receiptModel.GetProperties(), p => p.Name is "Payload" or "WorkflowInstanceId" or "AttemptId" or "BookmarkIdsJson" or "AuthorityOutstanding");
        Assert.Null(await host.Admissions.FindAsync(discard.ReceiptId!));
        Assert.DoesNotContain(await host.Admissions.FindRecoverableAsync(100, null), r => r.Id == discard.ReceiptId);
    }

    [Fact]
    public async Task UnprovisionedReceiptTableFailsBeforeWorkflowAdmissionOrDiscard()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, migrateReceipts: false);
        await Assert.ThrowsAnyAsync<Exception>(() => host.Discards.ValidateProvisioningAsync());
        await Assert.ThrowsAnyAsync<Exception>(() => host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(), SocketDiscardTestFixture.Now));
        await Assert.ThrowsAnyAsync<Exception>(async () => await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now));
        var subscription = await host.SubscriptionAsync();
        Assert.Equal(0, subscription.RetainedRecords);
        Assert.Equal(0, subscription.ActiveReservations);
    }

    [Theory]
    [InlineData("discard-history-shared", "__AdmissionMigrationsHistory")]
    [InlineData("discard-history-wrong", "__WrongSocketMigrationsHistory")]
    public async Task ActualWrongOrSharedHistoryIsRejectedEvenWhenExpectedMigrationExists(string caseId, string history)
    {
        _ = caseId;
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, receiptHistory: history, validateProvisioning: false);
        await using var receipts = await host.ReceiptContextAsync();
        var applied = (await receipts.Database.GetAppliedMigrationsAsync()).ToArray();
        Assert.Contains("20261008110000_InitialSlackSocketReceipts", applied);
        if (history == "__AdmissionMigrationsHistory")
        {
            Assert.Contains("20261008040000_InitialAdmission", applied);
        }
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Discards.ValidateProvisioningAsync());
        Assert.Equal("socket_receipt_history_or_schema_conflict", error.Message);
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.Equal(0, await receipts.Receipts.CountAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(), SocketDiscardTestFixture.Now));
    }

    [Theory]
    [InlineData("discard-layout-database", true)]
    [InlineData("discard-layout-schema", false)]
    public async Task MisconfiguredReceiptDatabaseOrSchemaCannotBeSilentlyRebound(string caseId, bool wrongDatabase)
    {
        _ = caseId;
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString);
        if (wrongDatabase)
        {
            connection.Database = "socket_fixture_wrong_database";
        }
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, migrateReceipts: false,
            receiptConnectionString: connection.ConnectionString, receiptSchema: wrongDatabase ? null : "OtherSocketSchema");
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Discards.ValidateProvisioningAsync());
        // A cached EF model can reject the schema first; both fail before any rebinding or write.
        Assert.Contains(error.Message, new[] { "socket_receipt_database_scope_conflict", "socket_receipt_history_or_schema_conflict" });
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Fact]
    public async Task ExplicitModelAndHistorySchemasDoNotDependOnReceiptSearchPath()
    {
        var connection = new NpgsqlConnectionStringBuilder(fixture.ConnectionString) { SearchPath = "unused_socket_search_path" };
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, receiptConnectionString: connection.ConnectionString);
        await host.Discards.ValidateProvisioningAsync();
        var committed = await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now);
        Assert.Equal(SlackSocketDiscardOutcome.Committed, committed.Outcome);
        Assert.NotNull(await host.ReceiptAsync(committed.ReceiptId!));
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Fact]
    public async Task ReceiptMigrationCannotCreateTheRequiredAdmissionTables()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(() => SocketDiscardTestFixture.CreateAsync(fixture, migrateAdmission: false));
        Assert.Equal("42P01", error.SqlState);
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""SELECT to_regclass('"Elsa"."AdmissionSubscriptions"') IS NULL""", connection);
        Assert.True((bool)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task ActualCommandsShareTheOwnerConnectionAndTransaction()
    {
        var observer = new SocketDiscardTransactionObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [observer]);
        var committed = await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now);
        Assert.Equal(SlackSocketDiscardOutcome.Committed, committed.Outcome);
        Assert.NotNull(observer.AdmissionConnection);
        Assert.NotNull(observer.AdmissionTransaction);
        Assert.Same(observer.AdmissionConnection, observer.ReceiptConnection);
        Assert.Same(observer.AdmissionTransaction, observer.ReceiptTransaction);
    }

    [Fact]
    public async Task EnlistedContextDisposalLeavesTheOwnerTransactionUsable()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        await using var admission = await host.AdmissionContextAsync();
        await using var transaction = await admission.Database.BeginTransactionAsync();
        var connection = admission.Database.GetDbConnection();
        await using (var receipts = await host.Services.GetRequiredService<SlackSocketReceiptTransactions>().EnlistAsync(admission, default))
        {
            Assert.Same(connection, receipts.Database.GetDbConnection());
            Assert.False(await receipts.Receipts.AnyAsync());
        }
        var subscription = await admission.Subscriptions.SingleAsync();
        var expectedRevision = subscription.Revision + 1;
        subscription.Revision = expectedRevision;
        await admission.SaveChangesAsync();
        await transaction.CommitAsync();
        Assert.Equal(expectedRevision, (await host.SubscriptionAsync()).Revision);
    }

    [Fact]
    public async Task RetryingReceiptStrategyIsRejectedBeforeAnyDurableDecision()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, receiptRetries: true, validateProvisioning: false);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Discards.ValidateProvisioningAsync());
        Assert.Equal("socket_receipt_selected_provider_required", error.Message);
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now));
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
    }

    [Fact]
    public async Task ForeignScopeCannotReadCleanupCandidatesOrDeleteTheActualReceipt()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var committed = await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now);
        var foreign = new PostgreSqlSlackSocketDiscardStore(host.Services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>(),
            new AdmissionPersistenceScope("foreign-tenant", "foreign-environment"), host.Services.GetRequiredService<IAdmissionTransactionLock>(),
            host.Services.GetRequiredService<SlackSocketReceiptTransactions>());
        Assert.Empty(await foreign.FindCleanupCandidatesAsync(100, null));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => foreign.CleanupAsync(committed.ReceiptId!, committed.Revision!.Value,
            SocketDiscardTestFixture.Now.AddDays(2), SocketDiscardTestFixture.Authority));
        Assert.Equal("socket_receipt_trusted_scope_conflict", error.Message);
        Assert.NotNull(await host.ReceiptAsync(committed.ReceiptId!));
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Theory]
    [InlineData("discard-mixed-workflow-first", true)]
    [InlineData("discard-mixed-discard-first", false)]
    public async Task MixedKindContendersShareTheActualSubscriptionTransaction(string caseId, bool workflowFirst)
    {
        _ = caseId;
        var gate = new SocketDiscardCommitGate();
        var observer = new SocketDiscardLockObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [gate, observer]);
        var request = await host.RequestAsync("shared-provider-event");
        gate.Arm();
        observer.Arm();
        Task<AdmissionResult>? workflow = null;
        Task<SlackSocketDiscardResult>? discard = null;
        try
        {
            if (workflowFirst)
            {
                workflow = host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(request.Event.ProviderEventId), SocketDiscardTestFixture.Now);
            }
            else
            {
                discard = host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
            }
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            if (workflowFirst)
            {
                discard = host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
            }
            else
            {
                workflow = host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(request.Event.ProviderEventId), SocketDiscardTestFixture.Now);
            }
            await observer.Contender.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(workflowFirst ? discard!.IsCompleted : workflow!.IsCompleted);
        }
        finally
        {
            await SocketDiscardTestFixture.ReleaseAndAwaitAsync(gate.Release, workflow, discard);
        }
        Assert.Equal(workflowFirst ? AdmissionOutcome.Committed : AdmissionOutcome.Quarantined, workflow!.Result.Outcome);
        Assert.Equal(workflowFirst ? SlackSocketDiscardOutcome.Quarantined : SlackSocketDiscardOutcome.Committed, discard!.Result.Outcome);
        var subscription = await host.SubscriptionAsync();
        Assert.Equal(1, subscription.RetainedRecords);
        Assert.Equal(workflowFirst ? 1 : 0, subscription.ActiveReservations);
        await using var receipts = await host.ReceiptContextAsync();
        await using var admission = await host.AdmissionContextAsync();
        Assert.Equal(workflowFirst ? 0 : 1, await receipts.Receipts.CountAsync());
        Assert.Equal(workflowFirst ? 1 : 0, await admission.Admissions.CountAsync());
    }

    [Fact]
    public async Task SameKindContendersChargeRetainedCapacityOnce()
    {
        var gate = new SocketDiscardCommitGate();
        var observer = new SocketDiscardLockObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [gate, observer]);
        var request = await host.RequestAsync();
        gate.Arm();
        observer.Arm();
        var first = host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        Task<SlackSocketDiscardResult>? second = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            second = host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
            await observer.Contender.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(second.IsCompleted);
        }
        finally
        {
            await SocketDiscardTestFixture.ReleaseAndAwaitAsync(gate.Release, first, second);
        }
        Assert.Equal(SlackSocketDiscardOutcome.Committed, first.Result.Outcome);
        Assert.Equal(SlackSocketDiscardOutcome.Duplicate, second!.Result.Outcome);
        Assert.Equal(first.Result.ReceiptId, second.Result.ReceiptId);
        var subscription = await host.SubscriptionAsync();
        Assert.Equal(1, subscription.RetainedRecords);
        Assert.Equal(0, subscription.ActiveReservations);
    }

    [Theory]
    [InlineData("discard-collision-payload", "payload")]
    [InlineData("discard-collision-reason", "reason")]
    [InlineData("discard-collision-binding", "binding")]
    [InlineData("discard-collision-configuration", "configuration")]
    [InlineData("discard-collision-epoch", "epoch")]
    public async Task ChangedIdentityContentCannotOverwriteAReceipt(string caseId, string change)
    {
        _ = caseId;
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var original = await host.RequestAsync();
        var committed = await host.Discards.RecordDiscardAsync(original, SocketDiscardTestFixture.Now);
        var changed = change switch
        {
            "payload" => original with { Event = original.Event with { Payload = "different-normalized-discard" } },
            "reason" => original with { Reason = SlackSocketDiscardReason.Edit },
            "binding" => original with { BindingFingerprint = AdmissionHash.Compute("different-listener-binding") },
            "configuration" => original with { ExpectedConfigurationFingerprint = AdmissionHash.Compute("different-subscription-configuration") },
            "epoch" => original with { ExpectedActivationEpoch = original.ExpectedActivationEpoch + 1 },
            _ => throw new ArgumentOutOfRangeException(nameof(change))
        };
        var quarantined = await host.Discards.RecordDiscardAsync(changed, SocketDiscardTestFixture.Now);
        Assert.Equal(SlackSocketDiscardOutcome.Quarantined, quarantined.Outcome);
        Assert.False(quarantined.AcknowledgementEligible);
        var row = (await host.ReceiptAsync(committed.ReceiptId!))!;
        Assert.Equal(original.BindingFingerprint, row.BindingFingerprint);
        Assert.Equal(original.Reason, row.Reason);
        Assert.Equal(AdmissionEventFingerprint.Compute(original.Event), row.EventFingerprint);
        Assert.Equal(1, row.Revision);
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Fact]
    public async Task CorruptedDualTableHistoryIsNeverAcceptedAsEitherDuplicate()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var request = await host.RequestAsync();
        var receipt = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        // Deliberately simulate a pre-existing unsupported writer. The supported store always uses its reader.
        var oldWriter = new EFCoreAdmissionStore(host.Services.GetRequiredService<IDbContextFactory<AdmissionElsaDbContext>>(),
            host.Services.GetRequiredService<AdmissionPersistenceScope>(), host.Services.GetRequiredService<IAdmissionTransactionLock>());
        var workflow = await oldWriter.AdmitAsync(AdmissionWorkerHost.Event(request.Event.ProviderEventId), SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Committed, workflow.Outcome);
        var before = await host.SubscriptionAsync();
        Assert.Equal(SlackSocketDiscardOutcome.Quarantined, (await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now)).Outcome);
        var admission = await host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(request.Event.ProviderEventId), SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Quarantined, admission.Outcome);
        Assert.Null(admission.AdmissionId);
        Assert.NotNull(await host.ReceiptAsync(receipt.ReceiptId!));
        Assert.NotNull(await host.Admissions.FindAsync(workflow.AdmissionId!));
        Assert.Equal(before.Revision, (await host.SubscriptionAsync()).Revision);
    }

    [Fact]
    public async Task DiscardIgnoresActiveCapacityButHonorsSharedRetainedCapacity()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, AdmissionWorkerHost.Configuration(activeCapacity: 1, retainedCapacity: 2));
        Assert.Equal(AdmissionOutcome.Committed, (await host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(), SocketDiscardTestFixture.Now)).Outcome);
        var request = await host.RequestAsync();
        Assert.Equal(SlackSocketDiscardOutcome.Committed, (await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now)).Outcome);
        Assert.Equal(SlackSocketDiscardOutcome.CapacityExceeded,
            (await host.Discards.RecordDiscardAsync(await host.RequestAsync("retained-overflow"), SocketDiscardTestFixture.Now)).Outcome);
        var subscription = await host.SubscriptionAsync();
        Assert.Equal(1, subscription.ActiveReservations);
        Assert.Equal(2, subscription.RetainedRecords);
    }

    [Fact]
    public async Task UnknownCommitPropagatesAndOnlyAFreshDefiniteDuplicateQualifies()
    {
        var unknown = new SocketDiscardUnknownCommit();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [unknown]);
        var request = await host.RequestAsync();
        unknown.Arm();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now));
        Assert.Equal("socket_fixture_commit_response_unknown", error.Message);
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
        var duplicate = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        Assert.Equal(SlackSocketDiscardOutcome.Duplicate, duplicate.Outcome);
        Assert.True(duplicate.AcknowledgementEligible);
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Fact]
    public async Task BothContextWritesRollBackWhenTheCounterSaveFails()
    {
        var failure = new SocketDiscardCounterSaveFailure();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [failure]);
        var request = await host.RequestAsync();
        failure.Arm();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now));
        Assert.Equal("socket_fixture_counter_save_failed", error.Message);
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.Equal(SlackSocketDiscardOutcome.Committed, (await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now)).Outcome);
    }

    [Fact]
    public async Task CleanupUsesCeiledImmutableHorizonAndReleasesExactlyOnce()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var request = await host.RequestAsync();
        var discard = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now.AddTicks(1));
        var row = (await host.ReceiptAsync(discard.ReceiptId!))!;
        Assert.Equal(SocketDiscardTestFixture.Now.AddTicks(10), row.DecisionAt);
        var boundary = row.DecisionAt + AdmissionWorkerHost.Configuration().Policy.IdentityHorizon;
        Assert.False(await host.Discards.CleanupAsync(row.Id, row.Revision, boundary.AddTicks(-1), SocketDiscardTestFixture.Authority));
        Assert.False(await host.Discards.CleanupAsync(row.Id, row.Revision + 1, boundary, SocketDiscardTestFixture.Authority));
        Assert.False(await host.Discards.CleanupAsync(row.Id, row.Revision, boundary, "wrong-cleanup-authority"));
        Assert.Equal(1, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.True(await host.Discards.CleanupAsync(row.Id, row.Revision, boundary, SocketDiscardTestFixture.Authority));
        Assert.False(await host.Discards.CleanupAsync(row.Id, row.Revision, boundary, SocketDiscardTestFixture.Authority));
        Assert.Null(await host.ReceiptAsync(row.Id));
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        var old = await host.Discards.RecordDiscardAsync(request, boundary);
        Assert.Equal(SlackSocketDiscardOutcome.Quarantined, old.Outcome);
        Assert.False(old.AcknowledgementEligible);
    }

    [Fact]
    public async Task PolicyEditCannotShortenAnExistingReceiptHorizon()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var discard = await host.Discards.RecordDiscardAsync(await host.RequestAsync(), SocketDiscardTestFixture.Now);
        var subscription = await host.SubscriptionAsync();
        var withdrawn = (await host.Admissions.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var configuration = withdrawn.Configuration;
        var shortened = configuration with { Policy = configuration.Policy with { IdentityHorizon = TimeSpan.FromHours(36) } };
        Assert.NotNull(await host.Admissions.ReconfigureAsync(shortened, withdrawn.Revision));
        Assert.False(await host.Discards.CleanupAsync(discard.ReceiptId!, discard.Revision!.Value,
            SocketDiscardTestFixture.Now.AddHours(37), SocketDiscardTestFixture.Authority));
        Assert.NotNull(await host.ReceiptAsync(discard.ReceiptId!));
        Assert.True(await host.Discards.CleanupAsync(discard.ReceiptId!, discard.Revision.Value,
            SocketDiscardTestFixture.Now.AddDays(2), SocketDiscardTestFixture.Authority));
    }

    [Fact]
    public async Task CleanedReceiptCannotBecomeEligibleThroughEventAgeWidening()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var request = await host.RequestAsync();
        var committed = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        Assert.True(await host.Discards.CleanupAsync(committed.ReceiptId!, committed.Revision!.Value,
            SocketDiscardTestFixture.Now.AddDays(2), SocketDiscardTestFixture.Authority));
        var subscription = await host.SubscriptionAsync();
        var withdrawn = (await host.Admissions.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        var widened = withdrawn.Configuration with
        {
            Policy = withdrawn.Configuration.Policy with { MaximumEventAge = TimeSpan.FromDays(3), IdentityHorizon = TimeSpan.FromDays(4) }
        };
        Assert.Null(await host.Admissions.ReconfigureAsync(widened, withdrawn.Revision));
        var unchanged = await host.SubscriptionAsync();
        Assert.Equal(withdrawn.Revision, unchanged.Revision);
        Assert.Equal(withdrawn.ConfigurationFingerprint, unchanged.ConfigurationFingerprint);
        var verified = (await host.Admissions.VerifyBootstrapAsync(unchanged.Id, unchanged.Revision, unchanged.ConfigurationFingerprint))!;
        Assert.NotNull(await host.Admissions.ActivateAsync(verified.Id, verified.Revision, SocketDiscardTestFixture.Now.AddDays(2)));
        var current = await host.SubscriptionAsync();
        var redelivery = request with { ExpectedActivationEpoch = current.ActivationEpoch, ExpectedConfigurationFingerprint = current.ConfigurationFingerprint };
        Assert.Equal(SlackSocketDiscardOutcome.Quarantined, (await host.Discards.RecordDiscardAsync(redelivery, SocketDiscardTestFixture.Now.AddDays(2))).Outcome);
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
    }

    [Fact]
    public async Task BoundedCleanupCursorVisitsAllRowsAndRestartsForNewInsertions()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        for (var i = 0; i < 3; i++)
        {
            await host.Discards.RecordDiscardAsync(await host.RequestAsync("cursor-event-" + i), SocketDiscardTestFixture.Now);
        }
        var seen = new List<string>();
        string? cursor = null;
        while (true)
        {
            var page = await host.Discards.FindCleanupCandidatesAsync(1, cursor);
            if (page.Count == 0)
            {
                break;
            }
            var candidate = Assert.Single(page);
            Assert.DoesNotContain(candidate.Id, seen);
            seen.Add(candidate.Id);
            cursor = candidate.Id;
        }
        Assert.Equal(3, seen.Count);
        var inserted = await host.Discards.RecordDiscardAsync(await host.RequestAsync("cursor-later-insertion"), SocketDiscardTestFixture.Now);
        var restarted = await host.Discards.FindCleanupCandidatesAsync(100, null);
        Assert.Equal(4, restarted.Count);
        Assert.Contains(restarted, c => c.Id == inserted.ReceiptId);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.Discards.FindCleanupCandidatesAsync(0, null));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => host.Discards.FindCleanupCandidatesAsync(1001, null));
    }

    [Theory]
    [InlineData("discard-cleanup-duplicate", "duplicate")]
    [InlineData("discard-cleanup-fresh", "discard")]
    [InlineData("discard-cleanup-workflow", "workflow")]
    public async Task CleanupSerializesWithDuplicateAndNewAdmissionCapacity(string caseId, string contenderKind)
    {
        _ = caseId;
        var gate = new SocketDiscardCommitGate();
        var observer = new SocketDiscardLockObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture,
            AdmissionWorkerHost.Configuration(activeCapacity: 1, retainedCapacity: 1), [gate, observer]);
        var request = await host.RequestAsync();
        var discard = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        var now = SocketDiscardTestFixture.Now.AddDays(2);
        gate.Arm();
        observer.Arm();
        var cleanup = host.Discards.CleanupAsync(discard.ReceiptId!, discard.Revision!.Value, now, SocketDiscardTestFixture.Authority);
        Task<SlackSocketDiscardResult>? discardContender = null;
        Task<AdmissionResult>? workflowContender = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            if (contenderKind == "workflow")
            {
                workflowContender = host.Admissions.AdmitAsync(AdmissionWorkerHost.Event("fresh-workflow-after-cleanup") with { OccurredAt = now }, now);
            }
            else
            {
                var next = contenderKind == "discard" ? request with { Event = request.Event with { ProviderEventId = "fresh-after-cleanup", OccurredAt = now } } : request;
                discardContender = host.Discards.RecordDiscardAsync(next, now);
            }
            await observer.Contender.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(workflowContender?.IsCompleted ?? discardContender!.IsCompleted);
        }
        finally
        {
            await SocketDiscardTestFixture.ReleaseAndAwaitAsync(gate.Release, cleanup, discardContender, workflowContender);
        }
        Assert.True(cleanup.Result);
        if (contenderKind == "workflow")
        {
            Assert.Equal(AdmissionOutcome.Committed, workflowContender!.Result.Outcome);
            Assert.NotNull(await host.Admissions.FindAsync(workflowContender.Result.AdmissionId!));
        }
        else
        {
            Assert.Equal(contenderKind == "discard" ? SlackSocketDiscardOutcome.Committed : SlackSocketDiscardOutcome.Quarantined, discardContender!.Result.Outcome);
        }
        var subscription = await host.SubscriptionAsync();
        Assert.Equal(contenderKind == "duplicate" ? 0 : 1, subscription.RetainedRecords);
        Assert.Equal(contenderKind == "workflow" ? 1 : 0, subscription.ActiveReservations);
    }

    [Fact]
    public async Task DuplicateHoldingTheIdentityLockCompletesBeforeCleanupDeletesOnce()
    {
        var gate = new SocketDiscardDuplicateReadGate();
        var observer = new SocketDiscardLockObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [gate, observer]);
        var request = await host.RequestAsync();
        var committed = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        gate.Arm();
        observer.Arm();
        var now = SocketDiscardTestFixture.Now.AddDays(2);
        var duplicate = host.Discards.RecordDiscardAsync(request, now);
        Task<bool>? cleanup = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            cleanup = host.Discards.CleanupAsync(committed.ReceiptId!, committed.Revision!.Value, now, SocketDiscardTestFixture.Authority);
            await observer.Contender.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(cleanup.IsCompleted);
        }
        finally
        {
            await SocketDiscardTestFixture.ReleaseAndAwaitAsync(gate.Release, duplicate, cleanup);
        }
        Assert.Equal(SlackSocketDiscardOutcome.Duplicate, duplicate.Result.Outcome);
        Assert.Equal(committed.ReceiptId, duplicate.Result.ReceiptId);
        Assert.True(cleanup!.Result);
        Assert.Null(await host.ReceiptAsync(committed.ReceiptId!));
        Assert.Equal(0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.False(await host.Discards.CleanupAsync(committed.ReceiptId!, committed.Revision!.Value, now, SocketDiscardTestFixture.Authority));
    }

    [Theory]
    [InlineData("discard-time-skew-limit", 60, true)]
    [InlineData("discard-time-skew-over", 61, false)]
    [InlineData("discard-time-before-activation", -61, false)]
    public async Task DiscardTimeWindowPreservesExplicitActivationAndSkew(string caseId, int offsetSeconds, bool accepted)
    {
        _ = caseId;
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var request = await host.RequestAsync();
        request = request with { Event = request.Event with { OccurredAt = SocketDiscardTestFixture.Now.AddSeconds(offsetSeconds) } };
        var result = await host.Discards.RecordDiscardAsync(request, SocketDiscardTestFixture.Now);
        Assert.Equal(accepted, result.AcknowledgementEligible);
        Assert.Equal(accepted ? 1 : 0, (await host.SubscriptionAsync()).RetainedRecords);
        Assert.Equal(0, (await host.SubscriptionAsync()).ActiveReservations);
    }
}
