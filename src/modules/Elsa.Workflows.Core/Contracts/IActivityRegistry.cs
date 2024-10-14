using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Stores all activity descriptors available to the system.
/// </summary>
public interface IActivityRegistry : IActivityProvider
{
    /// <summary>
    /// Adds an activity descriptor to the registry.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    /// <param name="descriptor">The activity descriptor to add.</param>
    void Add(Type providerType, ActivityDescriptor descriptor);
    
    /// <summary>
    /// Removes an activity descriptor from the registry.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    /// <param name="descriptor">The activity descriptor to remove.</param>
    void Remove(Type providerType, ActivityDescriptor descriptor);
    
    /// <summary>
    /// Returns all activity descriptors in the registry.
    /// </summary>
    /// <returns>All activity descriptors in the registry.</returns>
    IEnumerable<ActivityDescriptor> ListAll();
    
    /// <summary>
    /// Returns all activity descriptors provided by the specified provider.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    /// <returns>All activity descriptors provided by the specified provider.</returns>
    IEnumerable<ActivityDescriptor> ListByProvider(Type providerType);
    
    /// <summary>
    /// Returns the activity descriptor for the specified activity type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <returns>The activity descriptor for the specified activity type.</returns>
    ActivityDescriptor? Find(string type);
    
    /// <summary>
    /// Returns the activity descriptor for the specified activity type and version.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <param name="version">The activity version.</param>
    /// <returns>The activity descriptor for the specified activity type and version.</returns>
    ActivityDescriptor? Find(string type, int version);
    
    /// <summary>
    /// Returns the activity descriptor matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The activity descriptor matching the specified predicate.</returns>
    ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate);
    
    /// <summary>
    /// Returns all activity descriptors matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>All activity descriptors matching the specified predicate.</returns>
    IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate);
    
    /// <summary>
    /// Registers an activity descriptor.
    /// </summary>
    /// <param name="descriptor">The activity descriptor to register.</param>
    void Register(ActivityDescriptor descriptor);
    
    /// <summary>
    /// Registers an activity type.
    /// </summary>
    /// <param name="activityType">The activity type to register.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task RegisterAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers multiple activity types.
    /// </summary>
    /// <param name="activityTypes">The activity types to register.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the activity descriptors in the registry by querying the specified activity providers.
    /// </summary>
    /// <param name="activityProviders">The activity providers used to retrieve the descriptors.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RefreshDescriptorsAsync(IEnumerable<IActivityProvider> activityProviders, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the activity descriptors in the registry by querying the specified activity provider.
    /// </summary>
    Task RefreshDescriptorsAsync(IActivityProvider activityProvider, CancellationToken cancellationToken = default);
}