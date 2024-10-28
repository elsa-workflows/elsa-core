using Elsa.Workflows.Models;

namespace Elsa.Workflows;

/// <summary>
/// Represents a service used to lookup activity descriptors in the activity registry.
/// </summary>
public class ActivityRegistryLookupService(IActivityRegistry activityRegistry, IEnumerable<IActivityProvider> providers) : IActivityRegistryLookupService
{
    /// <inheritdoc />
    public Task<ActivityDescriptor?> FindAsync(string type)
    {
        return FindAsync(() => activityRegistry.Find(type));
    }

    /// <inheritdoc />
    public Task<ActivityDescriptor?> FindAsync(string type, int version)
    {
        return FindAsync(() => activityRegistry.Find(type, version));
    }

    /// <inheritdoc />
    public Task<ActivityDescriptor?> FindAsync(Func<ActivityDescriptor, bool> predicate)
    {
        return FindAsync(() => activityRegistry.Find(predicate));
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate)
    {
        return activityRegistry.FindMany(predicate);
    }
    
    private async Task<ActivityDescriptor?> FindAsync(Func<ActivityDescriptor?> findPredicate)
    {
        var descriptor = findPredicate.Invoke();
        if (descriptor is not null)
            return descriptor;

        await activityRegistry.RefreshDescriptorsAsync(providers);
        return findPredicate.Invoke();
    }
}