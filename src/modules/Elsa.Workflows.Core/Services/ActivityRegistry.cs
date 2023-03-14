using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityRegistry : IActivityRegistry
{
    private readonly IDictionary<Type, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new Dictionary<Type, ICollection<ActivityDescriptor>>();
    private readonly IDictionary<(string Type, int Version), ActivityDescriptor> _activityDescriptors = new Dictionary<(string Type, int Version), ActivityDescriptor>();

    /// <inheritdoc />
    public void Add(Type providerType, ActivityDescriptor descriptor) => Add(descriptor, GetOrCreateDescriptors(providerType));

    /// <inheritdoc />
    public void AddMany(Type providerType, IEnumerable<ActivityDescriptor> descriptors)
    {
        var target = GetOrCreateDescriptors(providerType);

        foreach (var descriptor in descriptors)
            Add(descriptor, target);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _activityDescriptors.Clear();
        _providedActivityDescriptors.Clear();
    }

    /// <inheritdoc />
    public void ClearProvider(Type providerType)
    {
        var descriptors = ListByProvider(providerType).ToList();

        foreach (var descriptor in descriptors)
            _activityDescriptors.Remove((descriptor.TypeName, descriptor.Version));

        _providedActivityDescriptors.Remove(providerType);
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListAll() => _activityDescriptors.Values;

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListByProvider(Type providerType) => _providedActivityDescriptors.TryGetValue(providerType, out var descriptors) ? descriptors : ArraySegment<ActivityDescriptor>.Empty;

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type) => _activityDescriptors.Values.Where(x => x.TypeName == type).MaxBy(x => x.Version);

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type, int version) => _activityDescriptors.TryGetValue((type, version), out var descriptor) ? descriptor : null;

    /// <inheritdoc />
    public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate) => _activityDescriptors.Values.FirstOrDefault(predicate);

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate) => _activityDescriptors.Values.Where(predicate);

    private void Add(ActivityDescriptor descriptor, ICollection<ActivityDescriptor> target)
    {
        _activityDescriptors.Add((descriptor.TypeName, descriptor.Version), descriptor);
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