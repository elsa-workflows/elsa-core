using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Services;

/// <inheritdoc />
public class ActivityRegistry : IActivityRegistry
{
    private readonly IActivityDescriber _activityDescriber;
    private readonly IEnumerable<IActivityDescriptorModifier> _modifiers;
    private readonly ISet<ActivityDescriptor> _manualActivityDescriptors = new HashSet<ActivityDescriptor>();
    private readonly IDictionary<Type, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new Dictionary<Type, ICollection<ActivityDescriptor>>();
    private readonly IDictionary<(string Type, int Version), ActivityDescriptor> _activityDescriptors = new Dictionary<(string Type, int Version), ActivityDescriptor>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRegistry"/> class.
    /// </summary>
    public ActivityRegistry(IActivityDescriber activityDescriber, IEnumerable<IActivityDescriptorModifier> modifiers)
    {
        _activityDescriber = activityDescriber;
        _modifiers = modifiers;
    }

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

    /// <inheritdoc />
    public void Register(ActivityDescriptor descriptor)
    {
        Add(GetType(), descriptor);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(Type activityType, CancellationToken cancellationToken)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);

        if (_activityDescriptors.Values.Any(x => x.TypeName == activityTypeName))
            return;

        var activityDescriptor = await _activityDescriber.DescribeActivityAsync(activityType, cancellationToken);
        Add(GetType(), activityDescriptor);
        _manualActivityDescriptors.Add(activityDescriptor);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default)
    {
        foreach (var activityType in activityTypes)
            await RegisterAsync(activityType, cancellationToken);
    }

    private void Add(ActivityDescriptor descriptor, ICollection<ActivityDescriptor> target)
    {
        foreach (var modifier in _modifiers) 
            modifier.Modify(descriptor);
        
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

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(_manualActivityDescriptors);
}