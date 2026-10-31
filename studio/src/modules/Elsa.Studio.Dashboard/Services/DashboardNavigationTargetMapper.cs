namespace Elsa.Studio.Dashboard.Services;

public static class DashboardNavigationTargetMapper
{
    public const string WorkflowInstances = "workflows/instances";
    public const string StructuredLogs = "diagnostics/structured-logs";
    public const string Console = "diagnostics/console";

    public static string? ForMetric(string metric)
    {
        return metric switch
        {
            "running" => WorkflowInstancesWith("status=Running"),
            "faulted" => WorkflowInstancesWith("subStatus=Faulted&hasIncidents=true"),
            "suspended" => WorkflowInstancesWith("subStatus=Suspended"),
            "interrupted" => WorkflowInstancesWith("subStatus=Interrupted"),
            "incidents" => WorkflowInstancesWith("hasIncidents=true"),
            _ => null
        };
    }

    public static string ForWorkflowInstance(string instanceId) => $"workflows/instances/{Uri.EscapeDataString(instanceId)}/view";

    public static string? ForFinding(string? targetKind, string? target)
    {
        return targetKind switch
        {
            "WorkflowInstances" => target switch
            {
                "faulted" => ForMetric("faulted"),
                "interrupted" => ForMetric("interrupted"),
                "incidents" => ForMetric("incidents"),
                _ => WorkflowInstances
            },
            "StructuredLogs" => target switch
            {
                "errors" => $"{StructuredLogs}?level=Error",
                _ => StructuredLogs
            },
            "ConsoleLogs" => target switch
            {
                "dropped" => $"{Console}?stream=stderr",
                _ => Console
            },
            _ => null
        };
    }

    private static string WorkflowInstancesWith(string query)
    {
        // TODO: Wire these query keys into the Workflow Instances page once filter deep links are supported there.
        return $"{WorkflowInstances}?{query}";
    }
}
