using System.Reflection;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.PortResolvers;

/// <summary>
/// Returns a list of outbound activities for a given activity by reflecting over its public properties matching <see cref="IActivity"/> and <c>ICollection{IActivity}</c>.
/// </summary>
public class PropertyBasedActivityResolver : IActivityResolver
{
    /// <inheritdoc />
    public int Priority => -1;

    /// <inheritdoc />
    public bool GetSupportsActivity(IActivity activity) => true;

    /// <inheritdoc />
    public ValueTask<IEnumerable<IActivity>> GetActivitiesAsync(IActivity activity, CancellationToken cancellationToken = default) =>
        new(GetActivities(activity)
            .Where(x => x != null)
            .Select(x => x!)
            .ToHashSet());


    private static IEnumerable<IActivity?> GetActivities(IActivity activity)
    {
        var activityType = activity.GetType();

        var ports =
            from prop in activityType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            where typeof(IActivity).IsAssignableFrom(prop.PropertyType) || typeof(IEnumerable<IActivity>).IsAssignableFrom(prop.PropertyType)
            let value = prop.GetValue(activity)
            let isCollection = GetPropertyIsCollection(prop.PropertyType)
            select isCollection ? (IEnumerable<IActivity>)value : new[] { (IActivity)value };

        return ports.SelectMany(x => x);
    }

    private static bool GetPropertyIsCollection(Type propertyType) => typeof(IEnumerable<IActivity>).IsAssignableFrom(propertyType);
}