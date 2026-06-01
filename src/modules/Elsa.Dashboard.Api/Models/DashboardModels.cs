namespace Elsa.Dashboard.Api.Models;

public record DashboardQuery(string? Range = null, bool IncludeSystem = false);

public record DashboardOverview
{
    public DashboardCapabilityStatus Capability { get; init; } = DashboardCapabilityStatus.Available;
    public string? BackendName { get; init; }
    public string? EnvironmentName { get; init; }
    public DashboardRuntimeStatus Runtime { get; init; } = new();
    public DashboardWorkflowInstanceMetrics WorkflowInstances { get; init; } = new();
    public DashboardDiagnosticsSummary Diagnostics { get; init; } = new();
    public string AppliedRange { get; init; } = DashboardRangeKeys.TwentyFourHours;
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

public record DashboardCapabilityStatus
{
    public static DashboardCapabilityStatus Available { get; } = new("Available");
    public static DashboardCapabilityStatus NotInstalled { get; } = new("NotInstalled");
    public static DashboardCapabilityStatus Unauthorized { get; } = new("Unauthorized");
    public static DashboardCapabilityStatus Unavailable { get; } = new("Unavailable");

    public DashboardCapabilityStatus(string status, string? reason = null)
    {
        Status = status;
        Reason = reason;
    }

    public string Status { get; init; }
    public string? Reason { get; init; }
}

public record DashboardRuntimeStatus
{
    public string Status { get; init; } = DashboardRuntimeStatusKeys.Unavailable;
    public bool IsAcceptingWork { get; init; }
    public int ActiveExecutionCycleCount { get; init; }
    public int IngressSourceCount { get; init; }
    public int FailedIngressSourceCount { get; init; }
    public DateTimeOffset? PausedAt { get; init; }
    public DateTimeOffset? DrainStartedAt { get; init; }
    public string? Reason { get; init; }
}

public record DashboardWorkflowInstanceMetrics
{
    public long Running { get; init; }
    public long Completed { get; init; }
    public long Faulted { get; init; }
    public long Suspended { get; init; }
    public long Interrupted { get; init; }
    public long IncidentBearing { get; init; }
    public TimeSpan? AverageDuration { get; init; }
}

public record DashboardDiagnosticsSummary
{
    public DashboardStructuredLogSummary StructuredLogs { get; init; } = new();
    public DashboardConsoleLogSummary ConsoleLogs { get; init; } = new();
}

public record DashboardStructuredLogSummary
{
    public DashboardCapabilityStatus Capability { get; init; } = DashboardCapabilityStatus.NotInstalled;
    public int SourceCount { get; init; }
    public int StaleSourceCount { get; init; }
    public int RecentErrorOrCriticalCount { get; init; }
    public long DroppedWriteCount { get; init; }
    public long DroppedEventCount { get; init; }
}

public record DashboardConsoleLogSummary
{
    public DashboardCapabilityStatus Capability { get; init; } = DashboardCapabilityStatus.NotInstalled;
    public int SourceCount { get; init; }
    public int StaleSourceCount { get; init; }
    public int RecentStderrCount { get; init; }
    public long DroppedLineCount { get; init; }
}

public record DashboardFinding
{
    public string Id { get; init; } = null!;
    public string Severity { get; init; } = DashboardFindingSeverity.Info;
    public string Message { get; init; } = null!;
    public string? TargetKind { get; init; }
    public string? Target { get; init; }
    public int Priority { get; init; }
}

public record DashboardNeedsAttentionResponse
{
    public IReadOnlyCollection<DashboardFinding> Findings { get; init; } = [];
    public DashboardCapabilityStatus Capability { get; init; } = DashboardCapabilityStatus.Available;
    public string AppliedRange { get; init; } = DashboardRangeKeys.TwentyFourHours;
}

public record DashboardTrendRequest
{
    public string? Range { get; init; }
    public string? Granularity { get; init; }
    public bool IncludeSystem { get; init; }
}

public record DashboardTrendResponse
{
    public IReadOnlyCollection<DashboardTrendBucket> Buckets { get; init; } = [];
    public string AppliedRange { get; init; } = DashboardRangeKeys.TwentyFourHours;
    public string Granularity { get; init; } = DashboardTrendGranularity.Hour;
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

public record DashboardTrendBucket
{
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
    public long CreatedOrStarted { get; init; }
    public long Finished { get; init; }
    public long Faulted { get; init; }
    public long Suspended { get; init; }
    public long IncidentBearing { get; init; }
}

public record DashboardRecentActivityItem
{
    public string InstanceId { get; init; } = null!;
    public string DefinitionId { get; init; } = null!;
    public string? WorkflowName { get; init; }
    public string Status { get; init; } = null!;
    public string SubStatus { get; init; } = null!;
    public int IncidentCount { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public DateTimeOffset? FinishedAt { get; init; }
}

public record DashboardRecentActivityResponse
{
    public IReadOnlyCollection<DashboardRecentActivityItem> Items { get; init; } = [];
    public string AppliedRange { get; init; } = DashboardRangeKeys.TwentyFourHours;
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

public record DashboardWorkflowHotspotsRequest
{
    public string? Range { get; init; }
    public string Metric { get; init; } = DashboardHotspotMetric.Faults;
    public int Take { get; init; } = 10;
    public bool IncludeSystem { get; init; }
}

public record DashboardWorkflowHotspotsResponse
{
    public IReadOnlyCollection<DashboardHotspot> Items { get; init; } = [];
    public string AppliedRange { get; init; } = DashboardRangeKeys.TwentyFourHours;
    public string Metric { get; init; } = DashboardHotspotMetric.Faults;
    public DateTimeOffset From { get; init; }
    public DateTimeOffset To { get; init; }
}

public record DashboardHotspot
{
    public string DefinitionId { get; init; } = null!;
    public string? WorkflowName { get; init; }
    public long Value { get; init; }
    public TimeSpan? AverageDuration { get; init; }
}

public static class DashboardRangeKeys
{
    public const string OneHour = "1h";
    public const string TwentyFourHours = "24h";
    public const string SevenDays = "7d";
}

public static class DashboardRuntimeStatusKeys
{
    public const string AcceptingWork = "AcceptingWork";
    public const string Paused = "Paused";
    public const string Draining = "Draining";
    public const string Unavailable = "Unavailable";
}

public static class DashboardTrendGranularity
{
    public const string Minute = "minute";
    public const string Hour = "hour";
    public const string Day = "day";
}

public static class DashboardFindingSeverity
{
    public const string Info = "Info";
    public const string Warning = "Warning";
    public const string Error = "Error";
    public const string Critical = "Critical";
    public const string Success = "Success";
}

public static class DashboardHotspotMetric
{
    public const string Faults = "Faults";
    public const string Executions = "Executions";
    public const string Incidents = "Incidents";
    public const string Duration = "Duration";
}
