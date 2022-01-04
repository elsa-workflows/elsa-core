using Elsa.Management.Contracts;
using Elsa.Management.Models;

namespace Elsa.Management.Services;

public class TriggerRegistry : ITriggerRegistry
{
    private readonly IDictionary<object, ICollection<TriggerDescriptor>> _providedTriggerDescriptors = new Dictionary<object, ICollection<TriggerDescriptor>>();
    private readonly IDictionary<string, TriggerDescriptor> _triggerDescriptors = new Dictionary<string, TriggerDescriptor>();

    public void Add(object provider, TriggerDescriptor descriptor) => Add(descriptor, GetOrCreateDescriptors(provider));

    public void AddMany(object provider, IEnumerable<TriggerDescriptor> descriptors)
    {
        var target = GetOrCreateDescriptors(provider);

        foreach (var descriptor in descriptors)
            Add(descriptor, target);
    }

    public void Clear()
    {
        _triggerDescriptors.Clear();
        _providedTriggerDescriptors.Clear();
    }

    public void ClearProvider(object provider)
    {
        var descriptors = ListByProvider(provider).ToList();

        foreach (var descriptor in descriptors)
            _triggerDescriptors.Remove(descriptor.NodeType);

        _providedTriggerDescriptors.Remove(provider);
    }

    public IEnumerable<TriggerDescriptor> ListAll() => _triggerDescriptors.Values;
    public IEnumerable<TriggerDescriptor> ListByProvider(object provider) => _providedTriggerDescriptors.TryGetValue(provider, out var descriptors) ? descriptors : ArraySegment<TriggerDescriptor>.Empty;
    public TriggerDescriptor? Find(Func<TriggerDescriptor, bool> predicate) => _triggerDescriptors.Values.FirstOrDefault(predicate);
    public TriggerDescriptor? Find(string triggerType) => _triggerDescriptors.TryGetValue(triggerType, out var descriptor) ? descriptor : null;

    private void Add(TriggerDescriptor descriptor, ICollection<TriggerDescriptor> target)
    {
        _triggerDescriptors.Add(descriptor.NodeType, descriptor);
        target.Add(descriptor);
    }

    private ICollection<TriggerDescriptor> GetOrCreateDescriptors(object provider)
    {
        if (_providedTriggerDescriptors.TryGetValue(provider, out var descriptors))
            return descriptors;

        descriptors = new List<TriggerDescriptor>();
        _providedTriggerDescriptors.Add(provider, descriptors);

        return descriptors;
    }
}