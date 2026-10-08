using System.Text.Json;
using Elsa.Slack.SocketMode;

namespace Elsa.Slack.Tests.Health;

public sealed class SlackSocketModeHealthTests
{
    [Fact]
    public void PublicSnapshotIsImmutablePayloadFreeAndIndependentOfLaterUpdates()
    {
        ISlackSocketModeHealth health = new SlackSocketModeHealth(3, 2);
        var before = health.GetSnapshot();
        var writer = (SlackSocketModeHealth)health;
        writer.SetState(SlackSocketModeHealthState.Connected, SlackSocketModeHealthReason.None);
        writer.RecordOutcome(SlackSocketModeHealthOutcome.Admitted);
        writer.SetWorkCounts(1, 1);
        Assert.Equal(new SlackSocketModeHealthSnapshot(SlackSocketModeHealthState.Inactive, SlackSocketModeHealthReason.None, 0, 0, 0, 0, 0, 0), before);
        Assert.Equal(new SlackSocketModeHealthSnapshot(SlackSocketModeHealthState.Connected, SlackSocketModeHealthReason.None, 1, 0, 0, 0, 1, 1), health.GetSnapshot());
        Assert.All(typeof(SlackSocketModeHealthSnapshot).GetProperties(), property =>
            Assert.True(property.PropertyType.IsEnum || property.PropertyType == typeof(long) || property.PropertyType == typeof(int)));
        Assert.Single(typeof(ISlackSocketModeHealth).GetMethods());
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(health.GetSnapshot()));
        Assert.All(json.RootElement.EnumerateObject(), property => Assert.Equal(JsonValueKind.Number, property.Value.ValueKind));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryCumulativeOutcomeSaturatesWithoutOverflow(int outcome)
    {
        var health = new SlackSocketModeHealth(3, 2);
        var selected = (SlackSocketModeHealthOutcome)outcome;
        health.RecordOutcome(selected, long.MaxValue - 1);
        health.RecordOutcome(selected, 2);
        health.RecordOutcome(selected, long.MaxValue);
        var snapshot = health.GetSnapshot();
        Assert.Equal(outcome == 0 ? long.MaxValue : 0, snapshot.Admitted);
        Assert.Equal(outcome == 1 ? long.MaxValue : 0, snapshot.Discarded);
        Assert.Equal(outcome == 2 ? long.MaxValue : 0, snapshot.Duplicate);
        Assert.Equal(outcome == 3 ? long.MaxValue : 0, snapshot.Rejected);
    }

    [Fact]
    public void ExplicitWorkBoundsCapGaugesAndInvalidUpdatesCannotChangeSnapshot()
    {
        var health = new SlackSocketModeHealth(3, 2);
        health.SetWorkCounts(int.MaxValue, int.MaxValue);
        var expected = health.GetSnapshot();
        Assert.Equal(3, expected.Queued);
        Assert.Equal(2, expected.Inflight);
        Assert.Throws<ArgumentOutOfRangeException>(() => health.SetWorkCounts(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => health.RecordOutcome(SlackSocketModeHealthOutcome.Admitted, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => health.RecordOutcome((SlackSocketModeHealthOutcome)42));
        Assert.Throws<ArgumentOutOfRangeException>(() => health.SetState((SlackSocketModeHealthState)42, SlackSocketModeHealthReason.None));
        Assert.Throws<ArgumentOutOfRangeException>(() => health.SetState(SlackSocketModeHealthState.Connected, (SlackSocketModeHealthReason)42));
        Assert.Equal(expected, health.GetSnapshot());
        health.SetWorkCounts(0, 0);
        Assert.Equal(0, health.GetSnapshot().Queued);
        Assert.Equal(0, health.GetSnapshot().Inflight);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlackSocketModeHealth(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlackSocketModeHealth(1, 0));
    }

    [Fact]
    public void ConcurrentUpdatesPreserveEveryOutcomeAndCoherentStatePairs()
    {
        var health = new SlackSocketModeHealth(3, 2);
        Parallel.For(0, 1000, index =>
        {
            health.RecordOutcome(SlackSocketModeHealthOutcome.Admitted);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Discarded);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Duplicate);
            health.RecordOutcome(SlackSocketModeHealthOutcome.Rejected);
            if (index % 2 == 0)
            {
                health.SetState(SlackSocketModeHealthState.Backpressured, SlackSocketModeHealthReason.Backpressure);
            }
            else
            {
                health.SetState(SlackSocketModeHealthState.ReconciliationRequired, SlackSocketModeHealthReason.AdmissionUncertainty);
            }
            var snapshot = health.GetSnapshot();
            Assert.True((snapshot.State, snapshot.Reason) is
                (SlackSocketModeHealthState.Backpressured, SlackSocketModeHealthReason.Backpressure) or
                (SlackSocketModeHealthState.ReconciliationRequired, SlackSocketModeHealthReason.AdmissionUncertainty));
        });
        var final = health.GetSnapshot();
        Assert.Equal(1000, final.Admitted);
        Assert.Equal(1000, final.Discarded);
        Assert.Equal(1000, final.Duplicate);
        Assert.Equal(1000, final.Rejected);
    }

    [Theory]
    [InlineData(SlackSocketModeHealthState.Inactive)]
    [InlineData(SlackSocketModeHealthState.Connecting)]
    [InlineData(SlackSocketModeHealthState.Connected)]
    [InlineData(SlackSocketModeHealthState.Backpressured)]
    [InlineData(SlackSocketModeHealthState.ReconciliationRequired)]
    [InlineData(SlackSocketModeHealthState.Stopped)]
    [InlineData(SlackSocketModeHealthState.Faulted)]
    public void FixedStatesAndReasonsCanBeReadWithoutTransportAuthority(SlackSocketModeHealthState state)
    {
        var health = new SlackSocketModeHealth(1, 1);
        foreach (var reason in Enum.GetValues<SlackSocketModeHealthReason>())
        {
            health.SetState(state, reason);
            Assert.Equal(state, health.GetSnapshot().State);
            Assert.Equal(reason, health.GetSnapshot().Reason);
        }
    }
}
