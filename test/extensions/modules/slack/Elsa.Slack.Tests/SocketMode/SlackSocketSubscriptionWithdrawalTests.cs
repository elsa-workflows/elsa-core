using System.Text.Json;
using Elsa.Slack.SocketMode;
using Elsa.Workflows.Admission;
using NSubstitute;

namespace Elsa.Slack.Tests.SocketMode;

public sealed class SlackSocketSubscriptionWithdrawalTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("row-id")]
    [InlineData("configuration-id")]
    [InlineData("tenant")]
    [InlineData("environment")]
    [InlineData("installation")]
    [InlineData("channel")]
    [InlineData("fingerprint")]
    [InlineData("content")]
    [InlineData("epoch")]
    [InlineData("reconciliation")]
    public async Task DifferentOrUncertainCapturedBindingNeverStartsWithdrawal(string condition)
    {
        var captured = SocketModeTestData.Subscription();
        AdmissionSubscription? current = Snapshot(captured);
        var actual = captured.Configuration;
        switch (condition)
        {
            case "missing": current = null; break;
            case "row-id": current.Id = "other"; break;
            case "configuration-id": actual = actual with { Id = "other" }; break;
            case "tenant": actual = actual with { TenantId = "other" }; break;
            case "environment": actual = actual with { EnvironmentId = "other" }; break;
            case "installation": actual = actual with { InstallationId = "other" }; break;
            case "channel": actual = actual with { ChannelId = "other" }; break;
            case "fingerprint": current.ConfigurationFingerprint = new string('b', 64); break;
            case "content": actual = actual with { DefinitionFingerprint = new string('b', 64) }; break;
            case "epoch": current.ActivationEpoch++; break;
            case "reconciliation": current.ReconciliationCode = "fixture_reconciliation"; break;
            default: throw new ArgumentOutOfRangeException(nameof(condition));
        }
        if (current != null)
        {
            current.ConfigurationJson = JsonSerializer.Serialize(actual);
        }
        var store = Substitute.For<IAdmissionStore>();
        store.FindSubscriptionAsync(captured.Configuration.Id, Arg.Any<CancellationToken>()).Returns(current);
        Assert.False(await new SlackSocketSubscriptionWithdrawal(SocketModeTestData.Configuration(subscriptions: [captured]), store)
            .WithdrawAsync(CancellationToken.None));
        await store.Received(1).FindSubscriptionAsync(captured.Configuration.Id, Arg.Any<CancellationToken>());
        await store.DidNotReceive().WithdrawAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EveryMemberUsesItsFreshRevisionAndAnInactiveRepeatDoesNotWrite()
    {
        var captured = new[] { SocketModeTestData.Subscription("subscription-a"), SocketModeTestData.Subscription("subscription-b") };
        var store = Substitute.For<IAdmissionStore>();
        foreach (var member in captured)
        {
            var current = Snapshot(member);
            var withdrawn = Snapshot(member, active: false);
            withdrawn.Revision++;
            store.FindSubscriptionAsync(member.Configuration.Id, Arg.Any<CancellationToken>()).Returns(current, withdrawn);
            store.WithdrawAsync(member.Configuration.Id, current.Revision, false, null, Arg.Any<CancellationToken>()).Returns(withdrawn);
        }
        var helper = new SlackSocketSubscriptionWithdrawal(SocketModeTestData.Configuration(subscriptions: captured), store);
        Assert.True(await helper.WithdrawAsync(CancellationToken.None));
        Assert.True(await helper.WithdrawAsync(CancellationToken.None));
        foreach (var member in captured)
        {
            await store.Received(2).FindSubscriptionAsync(member.Configuration.Id, Arg.Any<CancellationToken>());
            await store.Received(1).WithdrawAsync(member.Configuration.Id, 7, false, null, Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task StaleCasStopsTheCompleteSetWithoutReadbackOrRetry()
    {
        var captured = new[] { SocketModeTestData.Subscription("subscription-a"), SocketModeTestData.Subscription("subscription-b") };
        var store = Substitute.For<IAdmissionStore>();
        store.FindSubscriptionAsync(captured[0].Configuration.Id, Arg.Any<CancellationToken>()).Returns(Snapshot(captured[0]));
        store.WithdrawAsync(captured[0].Configuration.Id, 7, false, null, Arg.Any<CancellationToken>()).Returns((AdmissionSubscription?)null);
        Assert.False(await new SlackSocketSubscriptionWithdrawal(SocketModeTestData.Configuration(subscriptions: captured), store)
            .WithdrawAsync(CancellationToken.None));
        await store.Received(1).FindSubscriptionAsync(captured[0].Configuration.Id, Arg.Any<CancellationToken>());
        await store.Received(1).WithdrawAsync(captured[0].Configuration.Id, 7, false, null, Arg.Any<CancellationToken>());
        await store.DidNotReceive().FindSubscriptionAsync(captured[1].Configuration.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UnknownCommitEscapesWithoutReadbackOrRetry()
    {
        var captured = SocketModeTestData.Subscription();
        var store = Substitute.For<IAdmissionStore>();
        var unknown = new InvalidOperationException("fixture_unknown_commit");
        store.FindSubscriptionAsync(captured.Configuration.Id, Arg.Any<CancellationToken>()).Returns(Snapshot(captured));
        store.WithdrawAsync(captured.Configuration.Id, 7, false, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AdmissionSubscription?>(unknown));
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SlackSocketSubscriptionWithdrawal(SocketModeTestData.Configuration(subscriptions: [captured]), store).WithdrawAsync(CancellationToken.None));
        Assert.Same(unknown, thrown);
        await store.Received(1).FindSubscriptionAsync(captured.Configuration.Id, Arg.Any<CancellationToken>());
        await store.Received(1).WithdrawAsync(captured.Configuration.Id, 7, false, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancellationIgnoredByReadCannotStartCas()
    {
        var captured = SocketModeTestData.Subscription();
        var store = Substitute.For<IAdmissionStore>();
        using var canceled = new CancellationTokenSource();
        store.FindSubscriptionAsync(captured.Configuration.Id, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            canceled.Cancel();
            return Task.FromResult<AdmissionSubscription?>(Snapshot(captured));
        });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new SlackSocketSubscriptionWithdrawal(SocketModeTestData.Configuration(subscriptions: [captured]), store).WithdrawAsync(canceled.Token));
        await store.DidNotReceive().WithdrawAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    private static AdmissionSubscription Snapshot(SlackSocketSubscription captured, bool active = true) => new()
    {
        Id = captured.Configuration.Id, ConfigurationJson = JsonSerializer.Serialize(captured.Configuration),
        ConfigurationFingerprint = captured.Configuration.ConfigurationFingerprint, ActivationEpoch = captured.ActivationEpoch,
        Revision = 7, Active = active, BootstrapVerified = true
    };
}
