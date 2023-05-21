using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Contracts;

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
    /// Adds multiple activity descriptors to the registry.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    /// <param name="descriptors">The activity descriptors to add.</param>
    void AddMany(Type providerType, IEnumerable<ActivityDescriptor> descriptors);
    
    /// <summary>
    /// Clears all activity descriptors from the registry.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Clears all activity descriptors provided by the specified provider.
    /// </summary>
    /// <param name="providerType">The type of the activity provider.</param>
    void ClearProvider(Type providerType);
    
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
    Task RegisterAsync(Type activityType, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Registers multiple activity types.
    /// </summary>
    /// <param name="activityTypes">The activity types to register.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default);
}