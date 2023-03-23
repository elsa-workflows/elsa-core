using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Adds extension methods to <see cref="IActivityRegistry"/>.
/// </summary>
public static class ActivityRegistryExtensions
{
    /// <summary>
    /// Finds the activity descriptor for the specified activity.
    /// </summary>
    public static ActivityDescriptor? Find(this IActivityRegistry activityRegistry, IActivity activity) => activityRegistry.Find(activity.Type, activity.Version);
}