namespace Elsa.Slack.SocketMode;

/// <summary>Read-only, payload-free state of the configured Socket listener.</summary>
public interface ISlackSocketModeHealth
{
    SlackSocketModeHealthSnapshot GetSnapshot();
}

public enum SlackSocketModeHealthState { Inactive, Connecting, Connected, Backpressured, ReconciliationRequired, Stopped, Faulted }
public enum SlackSocketModeHealthReason { None, Configuration, Provisioning, Credentials, Transport, Protocol, Backpressure, Reconnect, Drain, Withdrawal, AdmissionUncertainty, Cleanup }

/// <summary>Cumulative outcomes saturate; queued/inflight gauges never exceed their configured bounds.</summary>
public sealed record SlackSocketModeHealthSnapshot(SlackSocketModeHealthState State, SlackSocketModeHealthReason Reason,
    long Admitted, long Discarded, long Duplicate, long Rejected, int Queued, int Inflight);
