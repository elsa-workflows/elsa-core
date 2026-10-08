using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Represents a service for looking up activity descriptors.
/// </summary>
public interface IActivityRegistryLookupService
{
    /// <summary>
    /// Returns the activity descriptor for the specified activity type.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <returns>The activity descriptor for the specified activity type.</returns>
    Task<ActivityDescriptor?> FindAsync(string type);

    /// <summary>
    /// Returns the activity descriptor for the specified activity type and version.
    /// </summary>
    /// <param name="type">The activity type.</param>
    /// <param name="version">The activity version.</param>
    /// <returns>The activity descriptor for the specified activity type and version.</returns>
    Task<ActivityDescriptor?> FindAsync(string type, int version);

    /// <summary>
    /// Returns the activity descriptor matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>The activity descriptor matching the specified predicate.</returns>
    Task<ActivityDescriptor?> FindAsync(Func<ActivityDescriptor, bool> predicate);
    
    /// <summary>
    /// Returns all activity descriptors matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The predicate to match.</param>
    /// <returns>All activity descriptors matching the specified predicate.</returns>
    IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate);
}