using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Services;

/// <summary>
/// Represents a service used to lookup activity descriptors in the activity registry.
/// </summary>
public class ActivityRegistryLookupService(IActivityRegistry activityRegistry, IEnumerable<IActivityProvider> providers) : IActivityRegistryLookupService
{
    /// <inheritdoc />
    public Task<ActivityDescriptor?> Find(string type)
    {
        return Find(() => activityRegistry.Find(type));
    }

    /// <inheritdoc />
    public Task<ActivityDescriptor?> Find(string type, int version)
    {
        return Find(() => activityRegistry.Find(type, version));
    }

    /// <inheritdoc />
    public Task<ActivityDescriptor?> Find(Func<ActivityDescriptor, bool> predicate)
    {
        return Find(() => activityRegistry.Find(predicate));
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate)
    {
        return activityRegistry.FindMany(predicate);
    }
    
    private async Task<ActivityDescriptor?> Find(Func<ActivityDescriptor?> findPredicate)
    {
        var descriptor = findPredicate.Invoke();
        if (descriptor is not null)
            return descriptor;

        await activityRegistry.RefreshDescriptors(providers);
        return findPredicate.Invoke();
    }
}