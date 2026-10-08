using Elsa.Slack.SocketMode;

namespace Elsa.Slack.Tests.SocketMode;

public class SlackSocketConfigurationTests
{
    [Fact]
    public void CapturedFanOutIsSortedCopiedAndReadOnly()
    {
        var first = SocketModeTestData.Subscription("a");
        var second = SocketModeTestData.Subscription("b");
        var source = new[] { second, first };
        var configuration = SocketModeTestData.Configuration(subscriptions: source);
        var fingerprint = configuration.BindingFingerprint;
        source[0] = SocketModeTestData.Subscription("replacement");
        Assert.Equal(new[] { "a", "b" }, configuration.Subscriptions.Select(x => x.Configuration.Id));
        Assert.Equal(fingerprint, configuration.BindingFingerprint);
        Assert.Equal(SocketModeTestData.Configuration(subscriptions: [first, second]).BindingFingerprint, fingerprint);
        var list = Assert.IsAssignableFrom<IList<SlackSocketSubscription>>(configuration.Subscriptions);
        Assert.Throws<NotSupportedException>(() => list[0] = source[0]);
        var newEpoch = first with { ActivationEpoch = 2 };
        Assert.NotEqual(fingerprint, SocketModeTestData.Configuration(subscriptions: [newEpoch, second]).BindingFingerprint);
    }

    [Theory]
    [InlineData("empty")]
    [InlineData("duplicate")]
    [InlineData("fanout-bound")]
    [InlineData("zero-epoch")]
    [InlineData("tenant")]
    [InlineData("environment")]
    [InlineData("installation")]
    [InlineData("channel")]
    public void RejectsIncompleteDuplicateOrForeignSubscriptionSets(string mutation)
    {
        var subscription = SocketModeTestData.Subscription();
        IReadOnlyList<SlackSocketSubscription> subscriptions = mutation switch
        {
            "empty" => [],
            "duplicate" => [subscription, subscription],
            "fanout-bound" => Enumerable.Range(0, 5).Select(x => SocketModeTestData.Subscription("subscription-" + x)).ToArray(),
            "zero-epoch" => [subscription with { ActivationEpoch = 0 }],
            "tenant" => [subscription with { Configuration = subscription.Configuration with { TenantId = "other" } }],
            "environment" => [subscription with { Configuration = subscription.Configuration with { EnvironmentId = "other" } }],
            "installation" => [subscription with { Configuration = subscription.Configuration with { InstallationId = "other" } }],
            "channel" => [subscription with { Configuration = subscription.Configuration with { ChannelId = "C_OTHER" } }],
            _ => throw new ArgumentOutOfRangeException(nameof(mutation))
        };
        Assert.Throws<ArgumentException>(() => SocketModeTestData.Configuration(subscriptions: subscriptions));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("bad\nteam", null)]
    [InlineData(null, "")]
    public void RequiresAnExplicitBoundedInstallationIdentity(string? teamId, string? enterpriseId) =>
        Assert.Throws<ArgumentException>(() => SocketModeTestData.Configuration(teamId: teamId, enterpriseId: enterpriseId));

    [Theory]
    [InlineData("x", 256, true)]
    [InlineData("x", 257, false)]
    [InlineData("é", 128, true)]
    [InlineData("é", 129, false)]
    public void RouteIdentifiersAreBoundedByUtf8Bytes(string character, int count, bool accepted)
    {
        var teamId = string.Concat(Enumerable.Repeat(character, count));
        if (!accepted)
        {
            Assert.Throws<ArgumentException>(() => SocketModeTestData.Configuration(teamId: teamId));
            return;
        }
        var configuration = SocketModeTestData.Configuration(teamId: teamId);
        var json = SocketModeTestData.Change(SocketModeTestData.Envelope(), x => x["payload"]!["team_id"] = teamId);
        Assert.Equal(teamId, SocketModeTestData.Parse(json, configuration).Event!.TeamId);
    }

    [Fact]
    public void EnterpriseOnlyRouteRequiresItsExactConfiguredIdentity()
    {
        var configuration = SocketModeTestData.Configuration(teamId: null, enterpriseId: "E_TEST");
        var json = SocketModeTestData.Change(SocketModeTestData.Envelope(), root =>
        {
            root["payload"]!.AsObject().Remove("team_id");
            root["payload"]!["enterprise_id"] = "E_TEST";
        });
        Assert.Equal("E_TEST", SocketModeTestData.Parse(json, configuration).Event!.EnterpriseId);
        Assert.Throws<InvalidDataException>(() => SocketModeTestData.Parse(SocketModeTestData.Change(json,
            x => x["payload"]!["enterprise_id"] = "E_OTHER"), configuration));
    }

    public static IEnumerable<object[]> InvalidLimits()
    {
        var limits = SocketModeTestData.Limits;
        yield return ["envelope-zero", limits with { MaximumEnvelopeBytes = 0 }];
        yield return ["envelope-over-technical", limits with { MaximumEnvelopeBytes = 1048577 }];
        yield return ["fragments", limits with { MaximumFragments = 0 }];
        yield return ["assembly-time", limits with { MaximumAssemblyTime = Timeout.InfiniteTimeSpan }];
        yield return ["depth", limits with { MaximumJsonDepth = 65 }];
        yield return ["pending-envelope", limits with { MaximumPendingEnvelopes = 0 }];
        yield return ["pending-ack", limits with { MaximumPendingAcknowledgements = 0 }];
        yield return ["concurrency-zero", limits with { MaximumAdmissionConcurrency = 0 }];
        yield return ["concurrency-over-queue", limits with { MaximumAdmissionConcurrency = 9 }];
        yield return ["fanout", limits with { MaximumFanOut = 0 }];
        yield return ["reconnect-attempts", limits with { MaximumReconnectAttempts = 0 }];
        yield return ["reconnect-delay", limits with { ReconnectDelay = TimeSpan.Zero }];
        yield return ["reconnect-max-before-delay", limits with { MaximumReconnectDelay = TimeSpan.FromMilliseconds(1) }];
        yield return ["operation-timeout", limits with { OperationTimeout = TimeSpan.FromMilliseconds((double)int.MaxValue + 1) }];
        yield return ["drain-timeout", limits with { DrainTimeout = Timeout.InfiniteTimeSpan }];
    }

    [Theory]
    [MemberData(nameof(InvalidLimits))]
    public void EveryTransportBudgetMustBeExplicitFiniteAndConsistent(string caseId, SlackSocketModeLimits limits) =>
        Assert.Throws<ArgumentException>(() => SocketModeTestData.Configuration(limits));
}
