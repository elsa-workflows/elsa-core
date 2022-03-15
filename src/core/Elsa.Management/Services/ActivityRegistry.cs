using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Services;

public class ActivityRegistry : IActivityRegistry
{
    private readonly IDictionary<object, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new Dictionary<object, ICollection<ActivityDescriptor>>();
    private readonly IDictionary<string, ActivityDescriptor> _activityDescriptors = new Dictionary<string, ActivityDescriptor>();

    public void Add(object provider, ActivityDescriptor descriptor) => Add(descriptor, GetOrCreateDescriptors(provider));

    public void AddMany(object provider, IEnumerable<ActivityDescriptor> descriptors)
    {
        var target = GetOrCreateDescriptors(provider);

        foreach (var descriptor in descriptors)
            Add(descriptor, target);
    }

    public void Clear()
    {
        _activityDescriptors.Clear();
        _providedActivityDescriptors.Clear();
    }

    public void ClearProvider(object provider)
    {
        var descriptors = ListByProvider(provider).ToList();

        foreach (var descriptor in descriptors)
            _activityDescriptors.Remove(descriptor.ActivityType);

        _providedActivityDescriptors.Remove(provider);
    }

    public IEnumerable<ActivityDescriptor> ListAll() => _activityDescriptors.Values;
    public IEnumerable<ActivityDescriptor> ListByProvider(object provider) => _providedActivityDescriptors.TryGetValue(provider, out var descriptors) ? descriptors : ArraySegment<ActivityDescriptor>.Empty;
    public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate) => _activityDescriptors.Values.FirstOrDefault(predicate);
    public ActivityDescriptor? Find(string activityType) => _activityDescriptors.TryGetValue(activityType, out var descriptor) ? descriptor : null;

    private void Add(ActivityDescriptor descriptor, ICollection<ActivityDescriptor> target)
    {
        _activityDescriptors.Add(descriptor.ActivityType, descriptor);
        target.Add(descriptor);
    }

    private ICollection<ActivityDescriptor> GetOrCreateDescriptors(object provider)
    {
        if (_providedActivityDescriptors.TryGetValue(provider, out var descriptors))
            return descriptors;

        descriptors = new List<ActivityDescriptor>();
        _providedActivityDescriptors.Add(provider, descriptors);

        return descriptors;
    }
}