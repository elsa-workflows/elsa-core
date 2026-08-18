using Elsa.Bpmn.Hosting;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// The queue that keeps a scope evaluation raised while another one is applying its commands from running on top of it.
/// </summary>
public class BpmnScopeDispatcherTests
{
    private readonly BpmnScopeDispatcher _dispatcher = new();
    private readonly List<string> _log = [];

    [Fact(DisplayName = "An evaluation posted while one is in flight runs after it, not inside it")]
    public async Task PostFromInsideARunningEvaluation_IsQueued()
    {
        await _dispatcher.PostAsync(async () =>
        {
            _log.Add("outer:begin");
            await _dispatcher.PostAsync(() =>
            {
                _log.Add("inner");
                return default;
            });
            _log.Add("outer:end");
        });

        Assert.Equal(["outer:begin", "outer:end", "inner"], _log);
    }

    [Fact(DisplayName = "Evaluations run in the order they were posted")]
    public async Task QueuedEvaluations_RunInPostOrder()
    {
        await _dispatcher.PostAsync(async () =>
        {
            await _dispatcher.PostAsync(() => Add("first"));
            await _dispatcher.PostAsync(() => Add("second"));
            await Add("root");
        });

        Assert.Equal(["root", "first", "second"], _log);
    }

    [Fact(DisplayName = "An evaluation that throws drops what was queued behind it")]
    public async Task FailedEvaluation_DropsTheRest()
    {
        // Everything queued behind a failed evaluation was planned against a state that no longer describes the
        // instance, and the failure is on its way to the incident strategy.
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await _dispatcher.PostAsync(async () =>
        {
            await _dispatcher.PostAsync(() => Add("queued"));
            throw new InvalidOperationException("boom");
        }));

        Assert.Empty(_log);
        Assert.False(_dispatcher.IsDraining);

        // The queue is usable again, and does not resurrect what was dropped.
        await _dispatcher.PostAsync(() => Add("after"));
        Assert.Equal(["after"], _log);
    }

    private ValueTask Add(string entry)
    {
        _log.Add(entry);
        return default;
    }
}
