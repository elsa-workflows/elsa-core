using Elsa.Studio.Dashboard.Models;
using MudBlazor;

namespace Elsa.Studio.Dashboard.Services;

public static class DashboardUiMapper
{
    public static Color RuntimeColor(string? status)
    {
        return status switch
        {
            DashboardRuntimeStatusKeys.AcceptingWork => Color.Success,
            DashboardRuntimeStatusKeys.Paused => Color.Warning,
            DashboardRuntimeStatusKeys.Draining => Color.Info,
            _ => Color.Error
        };
    }

    public static string RuntimeLabel(string? status)
    {
        return status switch
        {
            DashboardRuntimeStatusKeys.AcceptingWork => "Accepting work",
            DashboardRuntimeStatusKeys.Paused => "Paused",
            DashboardRuntimeStatusKeys.Draining => "Draining",
            _ => "Unavailable"
        };
    }

    public static Severity Severity(string? severity)
    {
        return severity switch
        {
            DashboardFindingSeverity.Success => MudBlazor.Severity.Success,
            DashboardFindingSeverity.Warning => MudBlazor.Severity.Warning,
            DashboardFindingSeverity.Error => MudBlazor.Severity.Error,
            DashboardFindingSeverity.Critical => MudBlazor.Severity.Error,
            _ => MudBlazor.Severity.Info
        };
    }

    public static Color SeverityColor(string? severity)
    {
        return severity switch
        {
            DashboardFindingSeverity.Success => Color.Success,
            DashboardFindingSeverity.Warning => Color.Warning,
            DashboardFindingSeverity.Error => Color.Error,
            DashboardFindingSeverity.Critical => Color.Error,
            _ => Color.Info
        };
    }

    public static string SeverityIcon(string? severity)
    {
        return severity switch
        {
            DashboardFindingSeverity.Success => Icons.Material.Outlined.CheckCircle,
            DashboardFindingSeverity.Warning => Icons.Material.Outlined.WarningAmber,
            DashboardFindingSeverity.Error => Icons.Material.Outlined.ErrorOutline,
            DashboardFindingSeverity.Critical => Icons.Material.Filled.Report,
            _ => Icons.Material.Outlined.Info
        };
    }

    public static string CapabilityLabel(DashboardCapabilityStatus? capability)
    {
        return capability?.Status switch
        {
            "Available" => "Available",
            "Unauthorized" => "No access",
            "NotInstalled" => "Not installed",
            _ => "Unavailable"
        };
    }

    public static Color CapabilityColor(DashboardCapabilityStatus? capability)
    {
        return capability?.Status switch
        {
            "Available" => Color.Success,
            "Unauthorized" => Color.Warning,
            "NotInstalled" => Color.Default,
            _ => Color.Error
        };
    }
}
