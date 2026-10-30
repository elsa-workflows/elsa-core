using System.Diagnostics.Metrics;

namespace Elsa.Connections.Features;

internal static class ConnectionLifecycleReconciliationMetrics
{
    public const string MeterName = "Elsa.Connections";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> CompletedPages = Meter.CreateCounter<long>("elsa.connections.reconciliation.pages.completed");
    private static readonly Counter<long> FailedPages = Meter.CreateCounter<long>("elsa.connections.reconciliation.pages.failed");
    private static readonly Counter<long> FailedScans = Meter.CreateCounter<long>("elsa.connections.reconciliation.scans.failed");
    private static readonly Counter<long> RecoveryRequired = Meter.CreateCounter<long>("elsa.connections.reconciliation.attention.recovery_required");
    private static readonly Counter<long> UnknownOffboarding = Meter.CreateCounter<long>("elsa.connections.reconciliation.attention.offboarding_outcome_unknown");

    public static void RecordCompletedPage() => CompletedPages.Add(1);
    public static void RecordFailedPage() => FailedPages.Add(1);
    public static void RecordFailedScan() => FailedScans.Add(1);
    public static void RecordRecoveryRequired() => RecoveryRequired.Add(1);
    public static void RecordUnknownOffboarding() => UnknownOffboarding.Add(1);
}
