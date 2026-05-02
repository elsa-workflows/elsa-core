using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class DrainOrchestratorPauseTests : DrainOrchestratorTestsBase
{
    [Fact(DisplayName = "Empty registry: drain completes within deadline")]
    public async Task EmptyRegistry()
    {
        var sut = BuildSut();

        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        Assert.Empty(outcome.Sources);
        Assert.Equal(0, outcome.ExecutionCyclesForceCancelledCount);
    }

    [Fact(DisplayName = "All sources pause successfully — outcome is CompletedWithinDeadline")]
    public async Task AllSourcesPauseCleanly()
    {
        var a = CreateSource("a");
        var b = CreateSource("b");
        Registry.Sources.Returns(new[] { a, b });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        await a.Received(1).PauseAsync(Arg.Any<CancellationToken>());
        await b.Received(1).PauseAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Source that throws is recorded as PauseFailed; other sources still complete")]
    public async Task ThrowingSourceIsIsolated()
    {
        var ok = CreateSource("ok");
        var bad = CreateSource("bad", pauseImpl: _ => throw new InvalidOperationException("bad day"));
        Registry.Sources.Returns(new[] { ok, bad });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        await Registry.Received(1).MarkPauseFailedAsync("bad", "exception", Arg.Any<Exception>());
        await ok.Received(1).PauseAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Source that hangs past per-source timeout is recorded as PauseFailed (timeout)")]
    public async Task HangingSourceIsTimedOut()
    {
        var hanger = CreateSource(
            "hanger",
            pauseTimeout: TimeSpan.FromMilliseconds(50),
            pauseImpl: ct =>
            {
                // Awaits the linked CT — when it fires, we observe the cancellation.
                return new ValueTask(Task.Delay(TimeSpan.FromSeconds(30), ct));
            });
        Registry.Sources.Returns(new[] { hanger });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        await Registry.Received(1).MarkPauseFailedAsync("hanger", "timeout", Arg.Any<Exception>());
    }

    [Fact(DisplayName = "Source that implements IForceStoppable is force-stopped after pause timeout")]
    public async Task ForceStoppableSourceIsEscalated()
    {
        var combined = Substitute.For<IIngressSource, IForceStoppable>();
        combined.Name.Returns("kafka");
        combined.PauseTimeout.Returns(TimeSpan.FromMilliseconds(50));
        combined.CurrentState.Returns(IngressSourceState.Running);
        combined.PauseAsync(Arg.Any<CancellationToken>()).Returns(ci => new ValueTask(Task.Delay(TimeSpan.FromSeconds(30), ci.Arg<CancellationToken>())));
        ((IForceStoppable)combined).ForceStopAsync(Arg.Any<CancellationToken>()).Returns(ValueTask.CompletedTask);
        Registry.Sources.Returns(new[] { combined });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        await ((IForceStoppable)combined).Received(1).ForceStopAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Source without IForceStoppable is left in PauseFailed without escalation")]
    public async Task NonForceStoppableSourceNotEscalated()
    {
        var bad = CreateSource("bad", pauseImpl: _ => throw new InvalidOperationException("nope"));
        Registry.Sources.Returns(new[] { bad });

        var sut = BuildSut();
        var outcome = await sut.DrainAsync(DrainTrigger.HostStopSignal);

        Assert.Equal(DrainResult.CompletedWithinDeadline, outcome.OverallResult);
        // No force-stop interface means no escalation possible — drain still completes.
        await Registry.Received(1).MarkPauseFailedAsync("bad", "exception", Arg.Any<Exception>());
    }
}
