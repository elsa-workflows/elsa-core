using System.Data.Common;
using System.Text.Json;
using Elsa.Slack.SocketMode;
using Elsa.Workflows.Admission;
using Elsa.Workflows.Admission.WorkerProcess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

[Collection("Connections PostgreSQL")]
public sealed class SocketSubscriptionWithdrawalTests(PostgreSqlConnectionsFixture fixture)
{
    [Fact]
    public async Task CompleteCapturedSetWithdrawsWithoutRetirementAndInactiveRepeatDoesNotWrite()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var first = await host.SubscriptionAsync();
        var second = await ActivateSecondAsync(host, first, "second-subscription");
        var helper = new SlackSocketSubscriptionWithdrawal(Configuration(first, second), host.Admissions);
        Assert.True(await helper.WithdrawAsync(CancellationToken.None));
        var snapshots = new Dictionary<string, string>();
        foreach (var captured in new[] { first, second })
        {
            var current = (await host.Admissions.FindSubscriptionAsync(captured.Id))!;
            Assert.False(current.Active);
            Assert.False(current.Retired);
            Assert.True(current.BootstrapVerified);
            Assert.Null(current.ReconciliationCode);
            Assert.Equal(captured.Revision + 1, current.Revision);
            Assert.Equal(captured.ActivationEpoch, current.ActivationEpoch);
            Assert.Equal(captured.ConfigurationJson, current.ConfigurationJson);
            Assert.Equal(captured.ActiveReservations, current.ActiveReservations);
            Assert.Equal(captured.RetainedRecords, current.RetainedRecords);
            snapshots.Add(current.Id, JsonSerializer.Serialize(current));
        }
        Assert.True(await helper.WithdrawAsync(CancellationToken.None));
        foreach (var (id, snapshot) in snapshots)
        {
            Assert.Equal(snapshot, JsonSerializer.Serialize(await host.Admissions.FindSubscriptionAsync(id)));
        }
        await AssertNoEventsAsync(host);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CapturedHelperCannotWithdrawReactivatedOrReconfiguredBinding(bool reconfigure)
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var captured = await host.SubscriptionAsync();
        var helper = new SlackSocketSubscriptionWithdrawal(Configuration(captured), host.Admissions);
        var activated = await ReactivateAsync(host.Admissions, captured, reconfigure);
        Assert.True(activated.Active);
        Assert.True(activated.ActivationEpoch > captured.ActivationEpoch);
        Assert.Equal(!reconfigure, captured.ConfigurationFingerprint == activated.ConfigurationFingerprint);
        var before = JsonSerializer.Serialize(activated);
        Assert.False(await helper.WithdrawAsync(CancellationToken.None));
        Assert.Equal(before, JsonSerializer.Serialize(await host.SubscriptionAsync()));
        await AssertNoEventsAsync(host);
    }

    [Fact]
    public async Task PartialCapturedSetCannotClaimCompletionOrWithdrawNewEpoch()
    {
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture);
        var first = await host.SubscriptionAsync();
        var second = await ActivateSecondAsync(host, first, "zz-second-subscription");
        var helper = new SlackSocketSubscriptionWithdrawal(Configuration(first, second), host.Admissions);
        var reactivated = await ReactivateAsync(host.Admissions, second, false);
        var snapshot = JsonSerializer.Serialize(reactivated);
        Assert.False(await helper.WithdrawAsync(CancellationToken.None));
        var withdrawn = (await host.SubscriptionAsync())!;
        Assert.False(withdrawn.Active);
        Assert.Equal(first.Revision + 1, withdrawn.Revision);
        Assert.Equal(first.ActivationEpoch, withdrawn.ActivationEpoch);
        Assert.Equal(snapshot, JsonSerializer.Serialize(await host.Admissions.FindSubscriptionAsync(second.Id)));
        await AssertNoEventsAsync(host);
    }

    [Fact]
    public async Task ConcurrentEventAfterFreshReadMakesWithdrawalCasFailWithoutRetry()
    {
        var gate = new WithdrawalReadGate();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [gate]);
        var captured = await host.SubscriptionAsync();
        var helper = new SlackSocketSubscriptionWithdrawal(Configuration(captured), host.Admissions);
        gate.Arm();
        var withdrawal = helper.WithdrawAsync(CancellationToken.None);
        string? snapshot = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
            var admitted = await host.Admissions.AdmitAsync(AdmissionWorkerHost.Event(), SocketDiscardTestFixture.Now);
            Assert.Equal(AdmissionOutcome.Committed, admitted.Outcome);
            var winner = await host.SubscriptionAsync();
            Assert.True(winner.Active);
            Assert.Equal(captured.Revision + 1, winner.Revision);
            Assert.Equal(captured.ActivationEpoch, winner.ActivationEpoch);
            Assert.Equal(1, winner.ActiveReservations);
            Assert.Equal(1, winner.RetainedRecords);
            snapshot = JsonSerializer.Serialize(winner);
        }
        finally
        {
            gate.Release.TrySetResult();
            await withdrawal.WaitAsync(TimeSpan.FromSeconds(30));
        }
        Assert.NotNull(snapshot);
        Assert.False(await withdrawal);
        Assert.Equal(snapshot, JsonSerializer.Serialize(await host.SubscriptionAsync()));
        await using var db = await host.AdmissionContextAsync();
        var record = Assert.Single(await db.Admissions.AsNoTracking().ToListAsync());
        Assert.Equal(AdmissionState.Admitted, record.State);
        Assert.Null(record.WorkflowInstanceId);
        Assert.False(record.AuthorityOutstanding);
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
    }

    [Fact]
    public async Task CommittedWithdrawalWithLostResponseRemainsUnknownToHelper()
    {
        var unknown = new SocketDiscardUnknownCommit();
        await using var host = await SocketDiscardTestFixture.CreateAsync(fixture, interceptors: [unknown]);
        var captured = await host.SubscriptionAsync();
        var helper = new SlackSocketSubscriptionWithdrawal(Configuration(captured), host.Admissions);
        unknown.Arm();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => helper.WithdrawAsync(CancellationToken.None));
        Assert.Equal("socket_fixture_commit_response_unknown", error.Message);
        var committed = await host.SubscriptionAsync();
        Assert.False(committed.Active);
        Assert.False(committed.Retired);
        Assert.Null(committed.ReconciliationCode);
        Assert.Equal(captured.Revision + 1, committed.Revision);
        Assert.Equal(captured.ActivationEpoch, committed.ActivationEpoch);
        Assert.Equal(captured.ConfigurationJson, committed.ConfigurationJson);
        await AssertNoEventsAsync(host);
    }

    private static async Task<AdmissionSubscription> ActivateSecondAsync(SocketDiscardTestFixture host, AdmissionSubscription first, string id)
    {
        await AdmissionTestLedger.ActivateAsync(host.Admissions, first.Configuration with { Id = id });
        return (await host.Admissions.FindSubscriptionAsync(id))!;
    }

    private static async Task<AdmissionSubscription> ReactivateAsync(IAdmissionStore store, AdmissionSubscription captured, bool reconfigure)
    {
        var withdrawn = await store.WithdrawAsync(captured.Id, captured.Revision, false, null);
        Assert.NotNull(withdrawn);
        if (reconfigure)
        {
            var configuration = captured.Configuration with { DefinitionFingerprint = AdmissionHash.Compute("changed-withdrawal-fixture-definition") };
            var changed = await store.ReconfigureAsync(configuration, withdrawn.Revision);
            Assert.NotNull(changed);
            withdrawn = await store.VerifyBootstrapAsync(changed.Id, changed.Revision, configuration.ConfigurationFingerprint);
            Assert.NotNull(withdrawn);
        }
        var activated = await store.ActivateAsync(withdrawn.Id, withdrawn.Revision, SocketDiscardTestFixture.Now);
        Assert.NotNull(activated);
        return activated;
    }

    private static SlackSocketModeConfiguration Configuration(params AdmissionSubscription[] subscriptions)
    {
        var first = subscriptions[0].Configuration;
        return new(first.TenantId, first.EnvironmentId, first.InstallationId, "connection", "A_TEST", "T_TEST", null, "U_SELF", first.ChannelId,
            subscriptions.Select(x => new SlackSocketSubscription(x.Configuration, x.ActivationEpoch)).ToArray(),
            new SlackSocketModeLimits(65536, 16, TimeSpan.FromSeconds(5), 16, 8, 8, 2, 4, 3,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5)));
    }

    private static async Task AssertNoEventsAsync(SocketDiscardTestFixture host)
    {
        await using var db = await host.AdmissionContextAsync();
        Assert.Equal(0, await db.Admissions.CountAsync());
        await using var receipts = await host.ReceiptContextAsync();
        Assert.Equal(0, await receipts.Receipts.CountAsync());
    }

    private sealed class WithdrawalReadGate : DbCommandInterceptor
    {
        private int _armed;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Arm() => Volatile.Write(ref _armed, 1);

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData,
            DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("\"AdmissionSubscriptions\" AS", StringComparison.Ordinal) &&
                Interlocked.Exchange(ref _armed, 0) == 1)
            {
                // Hold the actual PostgreSQL SELECT statement snapshot before EF returns
                // its old revision; another transaction commits a real admission meanwhile.
                Entered.TrySetResult();
                await Release.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
}
