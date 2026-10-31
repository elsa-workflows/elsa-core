using Elsa.Studio.Authorization;
using Elsa.Studio.Dashboard.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Dashboard.Widgets;

public record DashboardWidgetDescriptor(
    string Id,
    string Zone,
    int Order,
    Type ComponentType,
    string? Title = null,
    string? RequiredBackendCapability = null,
    string? PayloadKind = null)
{
    /// <summary>
    /// The permissions that let a user see the widget: it is shown to users holding any of them, and hidden from everyone
    /// else. Declare the view permission of the data the widget shows. A widget declaring none is shown to every user.
    /// </summary>
    public IReadOnlyCollection<Permission> RequiredPermissions { get; init; } = [];

    public bool IsVisible(DashboardWidgetContext context) => context.Snapshot != null;

    /// <summary>Whether the user may see the widget (see <see cref="RequiredPermissions"/>).</summary>
    public bool IsPermitted(UserPermissions permissions) => RequiredPermissions.Count == 0 || permissions.HasAny(RequiredPermissions);
}

public interface IDashboardWidgetRegistry
{
    void Add(DashboardWidgetDescriptor descriptor);

    IReadOnlyCollection<DashboardWidgetDescriptor> List();
}

public class DashboardWidgetRegistry : IDashboardWidgetRegistry
{
    private readonly object _lock = new();
    private readonly Dictionary<string, DashboardWidgetDescriptor> _descriptors = new(StringComparer.Ordinal);

    public void Add(DashboardWidgetDescriptor descriptor)
    {
        lock (_lock)
        {
            _descriptors[descriptor.Id] = descriptor;
        }
    }

    public IReadOnlyCollection<DashboardWidgetDescriptor> List()
    {
        lock (_lock)
            return _descriptors.Values.ToList();
    }
}

public record DashboardWidgetContext(
    string SelectedRange,
    bool Loading,
    DateTimeOffset? LastRefreshedAt,
    DashboardLoadStatus Status,
    string? Message,
    DashboardSnapshot? Snapshot,
    Func<Task> RefreshAsync,
    NavigationManager Navigation);

public static class DashboardWidgetZones
{
    public const string Metrics = "metrics";
    public const string Trend = "trend";
    public const string Activity = "activity";
    public const string Findings = "findings";

    // Legacy zones remain rendered by the shell so independently deployed companion modules continue to work.
    public const string PrimaryPanels = "primary-panels";
    public const string SecondaryPanels = "secondary-panels";
    public const string DiagnosticsStatus = "diagnostics-status";
}
