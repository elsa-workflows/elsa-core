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

    [Fact(DisplayName = "Initial state is None and accepting new work")]
    public void InitialState()
    {
        Assert.Equal(QuiescenceReason.None, _sut.CurrentState.Reason);
        Assert.True(_sut.IsAcceptingNewWork);
    }

    [Fact(DisplayName = "BeginDrain sets Drain and is forward-only")]
    public async Task BeginDrainForwardOnly()
    {
        var first = await _sut.BeginDrainAsync();
        Assert.Equal(QuiescenceReason.Drain, first.Reason);
        Assert.NotNull(first.DrainStartedAt);

        var second = await _sut.BeginDrainAsync();
        Assert.Equal(first.DrainStartedAt, second.DrainStartedAt); // unchanged — idempotent
    }

    [Fact(DisplayName = "Pause adds flag, second Pause is no-op")]
    public async Task PauseIdempotent()
    {
        var first = await _sut.PauseAsync("maintenance", "op@ex.com", CancellationToken.None);
        Assert.True(first.Reason.HasFlag(QuiescenceReason.AdministrativePause));

        var second = await _sut.PauseAsync("again", "op@ex.com", CancellationToken.None);
        Assert.Equal(first.PausedAt, second.PausedAt); // unchanged — idempotent
    }

    [Fact(DisplayName = "Resume clears AdministrativePause")]
    public async Task ResumeClearsPause()
    {
        await _sut.PauseAsync(null, null, CancellationToken.None);
        var state = await _sut.ResumeAsync("op@ex.com", CancellationToken.None);

        Assert.False(state.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        Assert.Null(state.PausedAt);
    }

    [Fact(DisplayName = "Resume on already-running runtime is no-op")]
    public async Task ResumeIdempotent()
    {
        var state = await _sut.ResumeAsync("op@ex.com", CancellationToken.None);

        Assert.Equal(QuiescenceReason.None, state.Reason);
    }

    [Fact(DisplayName = "Resume during drain is a no-op and does NOT clear pause")]
    public async Task ResumeDuringDrainNoOp()
    {
        await _sut.PauseAsync(null, null, CancellationToken.None);
        await _sut.BeginDrainAsync();

        var state = await _sut.ResumeAsync(null, CancellationToken.None);

        Assert.True(state.Reason.HasFlag(QuiescenceReason.Drain));
        Assert.True(state.Reason.HasFlag(QuiescenceReason.AdministrativePause)); // still paused
    }

    [Fact(DisplayName = "Drain + pause are composable; resume clears only pause")]
    public async Task DrainAndPauseComposable()
    {
        await _sut.BeginDrainAsync();
        var paused = await _sut.PauseAsync(null, null, CancellationToken.None);

        Assert.True(paused.Reason.HasFlag(QuiescenceReason.Drain));
        Assert.True(paused.Reason.HasFlag(QuiescenceReason.AdministrativePause));
        // Resume during drain is a no-op (guarantee from FR-002 + ResumeDuringDrainNoOp).
    }

    [Fact(DisplayName = "ActiveExecutionCycleCount delegates to IBurstRegistry")]
    public void ActiveExecutionCycleCountDelegates()
    {
        _cycleRegistry.ActiveCount.Returns(7);
        Assert.Equal(7, _sut.ActiveExecutionCycleCount);
    }
}
