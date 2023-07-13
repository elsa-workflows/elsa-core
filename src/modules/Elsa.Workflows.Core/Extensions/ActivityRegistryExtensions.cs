using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
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

    /// <summary>
    /// Finds the activity descriptor for the specified activity type.
    /// </summary>
    public static ActivityDescriptor? Find<T>(this IActivityRegistry activityRegistry) where T:IActivity => activityRegistry.Find(ActivityTypeNameHelper.GenerateTypeName<T>());
    
    /// <summary>
    /// Registers the specified activity type with the registry.
    /// </summary>
    /// <param name="activityRegistry">The activity registry.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <typeparam name="T">The type of the activity to register.</typeparam>
    public static async Task RegisterAsync<T>(this IActivityRegistry activityRegistry, CancellationToken cancellationToken = default) where T : IActivity => 
        await activityRegistry.RegisterAsync(typeof(T), cancellationToken);
}