using System.Text.Json;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class ExpectedAdmissionBindingTests(PostgreSqlConnectionsFixture fixture)
{
    [Theory]
    [InlineData("provider-expected-binding-reactivated", false)]
    [InlineData("provider-expected-binding-reconfigured", true)]
    public async Task CapturedBindingCannotCrossReactivationOrReconfiguration(string caseId, bool reconfigure)
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var captured = await host.SubscriptionAsync();
        var message = Bound(AdmissionWorkerHost.Event(), captured);
        var withdrawn = (await host.Admissions.WithdrawAsync(captured.Id, captured.Revision, false, null))!;
        Assert.NotNull(withdrawn);
        if (reconfigure)
        {
            var changed = captured.Configuration with { DefinitionFingerprint = AdmissionHash.Compute("changed-pinned-definition") };
            var configured = (await host.Admissions.ReconfigureAsync(changed, withdrawn.Revision))!;
            Assert.NotNull(configured);
            withdrawn = (await host.Admissions.VerifyBootstrapAsync(configured.Id, configured.Revision, changed.ConfigurationFingerprint))!;
            Assert.NotNull(withdrawn);
        }
        var activated = (await host.Admissions.ActivateAsync(withdrawn.Id, withdrawn.Revision, SocketDiscardTestFixture.Now))!;
        Assert.NotNull(activated);
        Assert.True(activated.ActivationEpoch > captured.ActivationEpoch);
        Assert.Equal(!reconfigure, activated.ConfigurationFingerprint == captured.ConfigurationFingerprint);
        var rejected = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Quarantined, rejected.Outcome);
        Assert.False(rejected.AcknowledgementEligible);
        await AssertUnchangedAsync(host, activated, 0);
        var current = await host.Admissions.AdmitAsync(Bound(message, activated), SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Committed, current.Outcome);
        var record = (await host.Admissions.FindAsync(current.AdmissionId!))!;
        Assert.Equal(activated.ActivationEpoch, record.ActivationEpoch);
        Assert.Equal(activated.ConfigurationFingerprint, record.ConfigurationFingerprint);
        await ObserveAsync(host, caseId, nameof(CapturedBindingCannotCrossReactivationOrReconfiguration), caseId, rejected, 1);
    }

    [Fact]
    public async Task DuplicateCannotAcknowledgeAnOlderOrWithdrawnEpoch()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var message = Bound(AdmissionWorkerHost.Event(), await host.SubscriptionAsync());
        var committed = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Committed, committed.Outcome);
        var original = JsonSerializer.Serialize(await host.Admissions.FindAsync(committed.AdmissionId!));
        var subscription = await host.SubscriptionAsync();
        var withdrawn = (await host.Admissions.WithdrawAsync(subscription.Id, subscription.Revision, false, null))!;
        Assert.NotNull(withdrawn);
        var inactive = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Inactive, inactive.Outcome);
        Assert.False(inactive.AcknowledgementEligible);
        var legacy = message with { ExpectedConfigurationFingerprint = null, ExpectedActivationEpoch = null };
        Assert.Equal(AdmissionOutcome.Duplicate, (await host.Admissions.AdmitAsync(legacy, SocketDiscardTestFixture.Now)).Outcome);
        await AssertUnchangedAsync(host, withdrawn, 1);
        var activated = (await host.Admissions.ActivateAsync(withdrawn.Id, withdrawn.Revision, SocketDiscardTestFixture.Now))!;
        Assert.NotNull(activated);
        var stale = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Quarantined, stale.Outcome);
        var oldDuplicate = await host.Admissions.AdmitAsync(Bound(message, activated), SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Quarantined, oldDuplicate.Outcome);
        Assert.False(oldDuplicate.AcknowledgementEligible);
        Assert.Equal(AdmissionOutcome.Duplicate, (await host.Admissions.AdmitAsync(legacy, SocketDiscardTestFixture.Now)).Outcome);
        Assert.Equal(original, JsonSerializer.Serialize(await host.Admissions.FindAsync(committed.AdmissionId!)));
        await AssertUnchangedAsync(host, activated, 1);
        await ObserveAsync(host, "provider-expected-binding-old-duplicate", nameof(DuplicateCannotAcknowledgeAnOlderOrWithdrawnEpoch), "default", oldDuplicate, 1);
    }

    [Theory]
    [InlineData("provider-expected-binding-inactive", "inactive")]
    [InlineData("provider-expected-binding-unverified", "unverified")]
    [InlineData("provider-expected-binding-retired", "retired")]
    [InlineData("provider-expected-binding-reconciliation", "reconciliation")]
    public async Task GuardedDuplicateRequiresReadySubscription(string caseId, string condition)
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var message = Bound(AdmissionWorkerHost.Event(), await host.SubscriptionAsync());
        var committed = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Committed, committed.Outcome);
        var original = JsonSerializer.Serialize(await host.Admissions.FindAsync(committed.AdmissionId!));
        await using (var db = await host.AdmissionContextAsync())
        {
            // Independent durable-state negatives discriminate each predicate even when Active remains true.
            var subscription = await db.Subscriptions.SingleAsync();
            switch (condition)
            {
                case "inactive": subscription.Active = false; break;
                case "unverified": subscription.BootstrapVerified = false; break;
                case "retired": subscription.Retired = true; break;
                case "reconciliation": subscription.ReconciliationCode = "fixture_reconciliation"; break;
                default: throw new ArgumentOutOfRangeException(nameof(condition));
            }
            await db.SaveChangesAsync();
        }
        var before = await host.SubscriptionAsync();
        var rejected = await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Inactive, rejected.Outcome);
        Assert.False(rejected.AcknowledgementEligible);
        Assert.Equal(original, JsonSerializer.Serialize(await host.Admissions.FindAsync(committed.AdmissionId!)));
        var legacy = message with { ExpectedConfigurationFingerprint = null, ExpectedActivationEpoch = null };
        Assert.Equal(AdmissionOutcome.Duplicate, (await host.Admissions.AdmitAsync(legacy, SocketDiscardTestFixture.Now)).Outcome);
        await AssertUnchangedAsync(host, before, 1);
        await ObserveAsync(host, caseId, nameof(GuardedDuplicateRequiresReadySubscription), caseId, rejected, 1);
    }

    [Fact]
    public async Task MalformedBindingPairRejectsWithoutLedgerMutation()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var before = await host.SubscriptionAsync();
        var message = AdmissionWorkerHost.Event();
        var invalid = new (string? Fingerprint, long? Epoch)[]
        {
            (null, before.ActivationEpoch), (before.ConfigurationFingerprint, null),
            ("", before.ActivationEpoch), (new string('a', 63), before.ActivationEpoch),
            (new string('g', 64), before.ActivationEpoch), (before.ConfigurationFingerprint, 0),
            (before.ConfigurationFingerprint, -1)
        };
        AdmissionResult? last = null;
        foreach (var (fingerprint, epoch) in invalid)
        {
            var rejected = await host.Admissions.AdmitAsync(message with
            {
                ExpectedConfigurationFingerprint = fingerprint, ExpectedActivationEpoch = epoch
            }, SocketDiscardTestFixture.Now);
            Assert.Equal(AdmissionOutcome.Rejected, rejected.Outcome);
            Assert.False(rejected.AcknowledgementEligible);
            Assert.Null(rejected.AdmissionId);
            last = rejected;
        }
        await AssertUnchangedAsync(host, before, 0);
        Assert.NotNull(last);
        await ObserveAsync(host, "provider-expected-binding-malformed", nameof(MalformedBindingPairRejectsWithoutLedgerMutation), "default", last, 0);
    }

    [Fact]
    public async Task ExpectedBindingPreservesLegacyEventContractAndDigest()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var legacy = AdmissionWorkerHost.Event();
        var (subscriptionId, installationId, channelId, providerEventId, occurredAt, human, loop, payload) = legacy;
        var reconstructed = new AdmissionEvent(subscriptionId, installationId, channelId, providerEventId, occurredAt, human, loop, payload);
        Assert.Equal(legacy, reconstructed);
        var bound = Bound(legacy, await host.SubscriptionAsync());
        Assert.Equal(AdmissionEventFingerprint.Compute(legacy), AdmissionEventFingerprint.Compute(bound));
        var committed = await host.Admissions.AdmitAsync(bound, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Committed, committed.Outcome);
        Assert.True(committed.AcknowledgementEligible);
        var duplicate = await host.Admissions.AdmitAsync(bound, SocketDiscardTestFixture.Now);
        Assert.Equal(AdmissionOutcome.Duplicate, duplicate.Outcome);
        Assert.True(duplicate.AcknowledgementEligible);
        Assert.Equal(AdmissionOutcome.Duplicate, (await host.Admissions.AdmitAsync(legacy, SocketDiscardTestFixture.Now)).Outcome);
        var record = (await host.Admissions.FindAsync(committed.AdmissionId!))!;
        Assert.Equal(AdmissionEventFingerprint.Compute(legacy), record.EventFingerprint);
        Assert.Equal(bound.ExpectedActivationEpoch!.Value, record.ActivationEpoch);
        var before = await host.SubscriptionAsync();
        Assert.Equal(1, before.ActiveReservations);
        Assert.Equal(1, before.RetainedRecords);
        await AssertUnchangedAsync(host, before, 1);
        await ObserveAsync(host, "provider-expected-binding-compatible", nameof(ExpectedBindingPreservesLegacyEventContractAndDigest), "default", duplicate, 1);
    }

    [Fact]
    public async Task BindingIsRecheckedAfterAcquiringSubscriptionLock()
    {
        var gate = new SocketDiscardCommitGate();
        var observer = new SocketDiscardLockObserver();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [gate, observer]);
        var message = Bound(AdmissionWorkerHost.Event(), await host.SubscriptionAsync());
        Assert.Equal(AdmissionOutcome.Committed, (await host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now)).Outcome);
        var captured = await host.SubscriptionAsync();
        gate.Arm();
        observer.Arm();
        Task<AdmissionSubscription?>? withdrawal = null;
        Task<AdmissionResult>? contender = null;
        try
        {
            withdrawal = host.Admissions.WithdrawAsync(captured.Id, captured.Revision, false, null);
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            contender = host.Admissions.AdmitAsync(message, SocketDiscardTestFixture.Now);
            await observer.Contender.Task.WaitAsync(TimeSpan.FromSeconds(30));
            Assert.False(contender.IsCompleted);
        }
        finally
        {
            await SocketDiscardTestFixture.ReleaseAndAwaitAsync(gate.Release, withdrawal, contender);
        }
        var withdrawn = (await withdrawal!)!;
        Assert.NotNull(withdrawn);
        var rejected = await contender!;
        Assert.Equal(AdmissionOutcome.Inactive, rejected.Outcome);
        Assert.False(rejected.AcknowledgementEligible);
        await AssertUnchangedAsync(host, withdrawn, 1);
        await ObserveAsync(host, "provider-expected-binding-locked-recheck", nameof(BindingIsRecheckedAfterAcquiringSubscriptionLock), "default", rejected, 1);
    }

    private static AdmissionEvent Bound(AdmissionEvent message, AdmissionSubscription subscription) => message with
    {
        ExpectedConfigurationFingerprint = subscription.ConfigurationFingerprint, ExpectedActivationEpoch = subscription.ActivationEpoch
    };

    private static async Task AssertUnchangedAsync(SocketDiscardTestFixture host, AdmissionSubscription before, int admissions)
    {
        Assert.Equal(JsonSerializer.Serialize(before), JsonSerializer.Serialize(await host.SubscriptionAsync()));
        await using var db = await host.AdmissionContextAsync();
        Assert.Equal(admissions, await db.Admissions.CountAsync());
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
    }

    private async Task ObserveAsync(SocketDiscardTestFixture host, string caseId, string method, string parameterId, AdmissionResult result, int admissions)
    {
        await using var db = await host.AdmissionContextAsync();
        var actualCount = await db.Admissions.CountAsync();
        Assert.Equal(admissions, actualCount);
        await AdmissionProofObservation.WriteAsync(fixture, caseId, $"{typeof(ExpectedAdmissionBindingTests).FullName}.{method}", parameterId, [],
            new Dictionary<string, bool> { ["bindingContractVerified"] = true },
            new Dictionary<string, object> { ["outcome"] = result.Outcome.ToString(), ["persistedAdmissions"] = actualCount });
    }
}
