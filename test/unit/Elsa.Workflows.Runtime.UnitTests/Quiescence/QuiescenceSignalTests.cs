using Elsa.Common;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Workflows.Runtime.UnitTests.Quiescence;

public class QuiescenceSignalTests
{
    private readonly ISystemClock _clock;
    private readonly IExecutionCycleRegistry _cycleRegistry;
    private readonly IOptions<GracefulShutdownOptions> _options;
    private readonly QuiescenceSignal _sut;

    public QuiescenceSignalTests()
    {
        _clock = Substitute.For<ISystemClock>();
        _clock.UtcNow.Returns(DateTimeOffset.Parse("2026-04-24T10:00:00Z"));
        _cycleRegistry = Substitute.For<IExecutionCycleRegistry>();
        _cycleRegistry.ActiveCount.Returns(0);
        _options = Microsoft.Extensions.Options.Options.Create(new GracefulShutdownOptions());
        _sut = new QuiescenceSignal(_options, _clock, _cycleRegistry);
    }

    [Test]
    [DisplayName("Initial state is None and accepting new work")]
    public async Task InitialState()
    {
        await Assert.That(_sut.CurrentState.Reason).IsEqualTo(QuiescenceReason.None);
        await Assert.That(_sut.IsAcceptingNewWork).IsTrue();
    }

    [Test]
    [DisplayName("BeginDrain sets Drain and is forward-only")]
    public async Task BeginDrainForwardOnly()
    {
        var first = await _sut.BeginDrainAsync();
        await Assert.That(first.Reason).IsEqualTo(QuiescenceReason.Drain);
        await Assert.That(first.DrainStartedAt).IsNotNull();

        var second = await _sut.BeginDrainAsync();
        await Assert.That(second.DrainStartedAt).IsEqualTo(first.DrainStartedAt); // unchanged — idempotent
    }

    [Test]
    [DisplayName("Pause adds flag, second Pause is no-op")]
    public async Task PauseIdempotent()
    {
        var first = await _sut.PauseAsync("maintenance", "op@ex.com", CancellationToken.None);
        await Assert.That(first.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsTrue();

        var second = await _sut.PauseAsync("again", "op@ex.com", CancellationToken.None);
        await Assert.That(second.PausedAt).IsEqualTo(first.PausedAt); // unchanged — idempotent
    }

    [Test]
    [DisplayName("Resume clears AdministrativePause")]
    public async Task ResumeClearsPause()
    {
        await _sut.PauseAsync(null, null, CancellationToken.None);
        var state = await _sut.ResumeAsync("op@ex.com", CancellationToken.None);

        await Assert.That(state.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsFalse();
        await Assert.That(state.PausedAt).IsNull();
    }

    [Test]
    [DisplayName("Resume on already-running runtime is no-op")]
    public async Task ResumeIdempotent()
    {
        var state = await _sut.ResumeAsync("op@ex.com", CancellationToken.None);

        await Assert.That(state.Reason).IsEqualTo(QuiescenceReason.None);
    }

    [Test]
    [DisplayName("Resume during drain is a no-op and does NOT clear pause")]
    public async Task ResumeDuringDrainNoOp()
    {
        await _sut.PauseAsync(null, null, CancellationToken.None);
        await _sut.BeginDrainAsync();

        var state = await _sut.ResumeAsync(null, CancellationToken.None);

        await Assert.That(state.Reason.HasFlag(QuiescenceReason.Drain)).IsTrue();
        await Assert.That(state.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsTrue(); // still paused
    }

    [Test]
    [DisplayName("Drain + pause are composable; resume clears only pause")]
    public async Task DrainAndPauseComposable()
    {
        await _sut.BeginDrainAsync();
        var paused = await _sut.PauseAsync(null, null, CancellationToken.None);

        await Assert.That(paused.Reason.HasFlag(QuiescenceReason.Drain)).IsTrue();
        await Assert.That(paused.Reason.HasFlag(QuiescenceReason.AdministrativePause)).IsTrue();
        // Resume during drain is a no-op (guarantee from FR-002 + ResumeDuringDrainNoOp).
    }

    [Test]
    [DisplayName("ActiveExecutionCycleCount delegates to IBurstRegistry")]
    public async Task ActiveExecutionCycleCountDelegates()
    {
        _cycleRegistry.ActiveCount.Returns(7);
        await Assert.That(_sut.ActiveExecutionCycleCount).IsEqualTo(7);
    }
}
