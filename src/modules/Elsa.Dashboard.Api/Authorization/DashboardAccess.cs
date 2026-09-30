using Elsa.Authorization;
using Elsa.Dashboard.Abstractions.Models;
using Elsa.Dashboard.Api.Permissions;
using Microsoft.AspNetCore.Http;

namespace Elsa.Dashboard.Api.Authorization;

/// <summary>
/// Decides what a caller may read from the dashboard. <c>dashboard:view</c> reads the whole operational overview; a
/// narrower permission reads only the data it guards, so a caller is never refused data they may read elsewhere.
/// </summary>
internal static class DashboardAccess
{
    /// <summary>The permission that reads every section of the dashboard.</summary>
    public static readonly DashboardPermission Overview = new(DashboardResourcePermissions.Dashboard, CoreVerbs.View);

    /// <summary>
    /// The permission that reads workflow instance data. Mirrors <c>WorkflowPermissions.Instances</c>: the dashboard
    /// module does not reference the workflows module, and a test pins the two together.
    /// </summary>
    public static readonly DashboardPermission WorkflowInstances = new("workflows/instances", CoreVerbs.View);

    /// <summary>
    /// Tells whether the caller may read data guarded by a permission: they hold <c>dashboard:view</c> or the permission
    /// itself. Allows everything when endpoint security is disabled, as <c>RequirePermission</c> does.
    /// </summary>
    public static Func<DashboardPermission, bool> CreateReadCheck(HttpContext context)
    {
        if (!EndpointSecurityOptions.SecurityIsEnabled)
            return _ => true;

        var evaluator = context.GetPermissionEvaluator();
        var user = context.User;

        return permission => evaluator.HasPermission(user, Overview.Resource, Overview.Verb)
                             || evaluator.HasPermission(user, permission.Resource, permission.Verb);
    }

    /// <summary>Tells whether the caller may read workflow instance data: trends, recent activity, findings and hotspots.</summary>
    public static bool CanReadInstances(HttpContext context) => CreateReadCheck(context)(WorkflowInstances);
}
