using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a binding between an owner activity's port to a child activity.
/// </summary>
public record ActivityPort(IActivity? Activity, ICollection<IActivity>? Activities, string PortName)
{
    public static ActivityPort FromActivity(IActivity activity, string portName) => new ActivityPort(activity, null, portName);
    public static ActivityPort FromActivities(IEnumerable<IActivity> activities, string portName) => new ActivityPort(null, activities.ToList(), portName);

    public IEnumerable<IActivity> GetActivities()
    {
        return Activity != null ? [Activity] : Activities!.ToList();
    }
}