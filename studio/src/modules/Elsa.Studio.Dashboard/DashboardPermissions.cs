using Elsa.Studio.Authorization;

namespace Elsa.Studio.Dashboard;

/// <summary>Backend permission resources guarding the dashboard APIs.</summary>
/// <remarks>
/// <c>dashboard:view</c> reads the whole operational overview. The dashboard API also serves each piece of data to callers
/// holding the view permission of that data, so a widget is visible to users holding any of <see cref="ForData"/>.
/// </remarks>
public static class DashboardPermissions
{
    /// <summary>The operational dashboard.</summary>
    public const string Dashboard = "dashboard";

    /// <summary>
    /// Workflow instances, which also read the dashboard's instance metrics, trends, recent activity, findings and hotspots.
    /// Mirrors <c>WorkflowPermissions.Instances</c>: this module does not reference the workflows module.
    /// </summary>
    public const string WorkflowInstances = "workflows/instances";

    /// <summary>The workflow runtime, which also reads the dashboard's runtime status. Mirrors <c>WorkflowPermissions.Runtime</c>.</summary>
    public const string WorkflowRuntime = "workflows/runtime";

    /// <summary>The permissions that read dashboard data guarded by <paramref name="resource"/>: <c>dashboard:view</c>, or viewing the data itself.</summary>
    public static IReadOnlyCollection<Permission> ForData(string resource) =>
        [new(Dashboard, PermissionVerbs.View), new(resource, PermissionVerbs.View)];
}
