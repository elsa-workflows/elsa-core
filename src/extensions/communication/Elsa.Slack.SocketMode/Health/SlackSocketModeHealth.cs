namespace Elsa.Slack.SocketMode;

internal enum SlackSocketModeHealthOutcome { Admitted, Discarded, Duplicate, Rejected }

// Only the owned coordinator updates health. The public contract cannot mutate listener state.
internal sealed class SlackSocketModeHealth : ISlackSocketModeHealth
{
    private readonly object _gate = new();
    private readonly int _maximumQueued;
    private readonly int _maximumInflight;
    private SlackSocketModeHealthState _state;
    private SlackSocketModeHealthReason _reason;
    private long _admitted;
    private long _discarded;
    private long _duplicate;
    private long _rejected;
    private int _queued;
    private int _inflight;
    private bool _reconciliationLatched;

    internal SlackSocketModeHealth(int maximumQueued, int maximumInflight)
    {
        if (maximumQueued <= 0 || maximumInflight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumQueued));
        }
        _maximumQueued = maximumQueued;
        _maximumInflight = maximumInflight;
    }

    public SlackSocketModeHealthSnapshot GetSnapshot()
    {
        lock (_gate)
        {
            return new(_state, _reason, _admitted, _discarded, _duplicate, _rejected, _queued, _inflight);
        }
    }

    internal void SetState(SlackSocketModeHealthState state, SlackSocketModeHealthReason reason)
    {
        if (!Enum.IsDefined(state) || !Enum.IsDefined(reason))
        {
            throw new ArgumentOutOfRangeException(nameof(state));
        }
        lock (_gate)
        {
            if (!_reconciliationLatched)
            {
                _state = state;
                _reason = reason;
            }
        }
    }

    // A parent failure survives later socket-generation Connected/Stopped callbacks.
    // A fresh listener lifetime requires a fresh health owner, never an implicit reset.
    internal void LatchReconciliation(SlackSocketModeHealthReason reason)
    {
        if (!Enum.IsDefined(reason) || reason == SlackSocketModeHealthReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }
        lock (_gate)
        {
            if (!_reconciliationLatched)
            {
                _reconciliationLatched = true;
                _state = SlackSocketModeHealthState.ReconciliationRequired;
                _reason = reason;
            }
        }
    }

    internal void RecordOutcome(SlackSocketModeHealthOutcome outcome, long count = 1)
    {
        if (!Enum.IsDefined(outcome) || count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(outcome));
        }
        lock (_gate)
        {
            switch (outcome)
            {
                case SlackSocketModeHealthOutcome.Admitted:
                    _admitted = Add(_admitted, count);
                    break;
                case SlackSocketModeHealthOutcome.Discarded:
                    _discarded = Add(_discarded, count);
                    break;
                case SlackSocketModeHealthOutcome.Duplicate:
                    _duplicate = Add(_duplicate, count);
                    break;
                case SlackSocketModeHealthOutcome.Rejected:
                    _rejected = Add(_rejected, count);
                    break;
            }
        }
    }

    internal void SetWorkCounts(int queued, int inflight)
    {
        if (queued < 0 || inflight < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(queued));
        }
        lock (_gate)
        {
            _queued = Math.Min(queued, _maximumQueued);
            _inflight = Math.Min(inflight, _maximumInflight);
        }
    }

    private static long Add(long value, long count) => count > long.MaxValue - value ? long.MaxValue : value + count;
}
