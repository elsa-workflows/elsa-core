namespace Elsa.Workflows.Models;

/// <summary>
/// Represents a binding between an owner activity's port and a child activity.
/// </summary>
public record ActivityPort(IActivity? Activity, ICollection<IActivity>? Activities, string PortName)
{
    /// <summary>
    /// Creates a new ActivityPort from the specified activity and port name.
    /// </summary>
    public static ActivityPort FromActivity(IActivity activity, string portName) => new(activity, null, portName);

    /// <summary>
    /// Creates a new ActivityPort from the specified activities and port name.
    /// </summary>
    public static ActivityPort FromActivities(IEnumerable<IActivity> activities, string portName) => new(null, activities.ToList(), portName);

    /// <summary>
    /// Returns the activities bound to the port. Returns a list even if the Activity is set and not Activities.
    /// </summary>
    public IEnumerable<IActivity> GetActivities()
    {
        return Activity != null ? [Activity] : Activities!.ToList();
    }
}