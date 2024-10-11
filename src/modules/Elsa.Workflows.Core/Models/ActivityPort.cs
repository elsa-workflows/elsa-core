namespace Elsa.Workflows.Models;

/// Represents a binding between an owner activity's port and a child activity.
public record ActivityPort(IActivity? Activity, ICollection<IActivity>? Activities, string PortName)
{
    /// Creates a new ActivityPort from the specified activity and port name.
    public static ActivityPort FromActivity(IActivity activity, string portName) => new(activity, null, portName);

    /// Creates a new ActivityPort from the specified activities and port name.
    public static ActivityPort FromActivities(IEnumerable<IActivity> activities, string portName) => new(null, activities.ToList(), portName);

    /// Returns the activities bound to the port. Returns a list even if the Activity is set and not Activities.
    public IEnumerable<IActivity> GetActivities()
    {
        return Activity != null ? [Activity] : Activities!.ToList();
    }
}