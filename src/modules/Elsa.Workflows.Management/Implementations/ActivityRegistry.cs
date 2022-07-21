using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Services;

namespace Elsa.Workflows.Management.Implementations;

public class ActivityRegistry : IActivityRegistry
{
    private readonly IDictionary<Type, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new Dictionary<Type, ICollection<ActivityDescriptor>>();
    private readonly IDictionary<string, ActivityDescriptor> _activityDescriptors = new Dictionary<string, ActivityDescriptor>();

    public void Add(Type providerType, ActivityDescriptor descriptor) => Add(descriptor, GetOrCreateDescriptors(providerType));

    public void AddMany(Type providerType, IEnumerable<ActivityDescriptor> descriptors)
    {
        var target = GetOrCreateDescriptors(providerType);

        foreach (var descriptor in descriptors)
            Add(descriptor, target);
    }

    public void Clear()
    {
        _activityDescriptors.Clear();
        _providedActivityDescriptors.Clear();
    }

    public void ClearProvider(Type providerType)
    {
        var descriptors = ListByProvider(providerType).ToList();

        foreach (var descriptor in descriptors)
            _activityDescriptors.Remove(descriptor.ActivityType);

        _providedActivityDescriptors.Remove(providerType);
    }

    public IEnumerable<ActivityDescriptor> ListAll() => _activityDescriptors.Values;
    public IEnumerable<ActivityDescriptor> ListByProvider(Type providerType) => _providedActivityDescriptors.TryGetValue(providerType, out var descriptors) ? descriptors : ArraySegment<ActivityDescriptor>.Empty;
    public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate) => _activityDescriptors.Values.FirstOrDefault(predicate);
    public ActivityDescriptor? Find(string activityType) => _activityDescriptors.TryGetValue(activityType, out var descriptor) ? descriptor : null;

    private void Add(ActivityDescriptor descriptor, ICollection<ActivityDescriptor> target)
    {
        _activityDescriptors.Add(descriptor.ActivityType, descriptor);
        target.Add(descriptor);
    }

    private ICollection<ActivityDescriptor> GetOrCreateDescriptors(Type provider)
    {
        if (_providedActivityDescriptors.TryGetValue(provider, out var descriptors))
            return descriptors;

        descriptors = new List<ActivityDescriptor>();
        _providedActivityDescriptors.Add(provider, descriptors);

        return descriptors;
    }
}