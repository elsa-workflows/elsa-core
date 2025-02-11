using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows;

/// <inheritdoc />
public class ActivityRegistry(IActivityDescriber activityDescriber, IEnumerable<IActivityDescriptorModifier> modifiers, ILogger<ActivityRegistry> logger) : IActivityRegistry
{
    private readonly ISet<ActivityDescriptor> _manualActivityDescriptors = new HashSet<ActivityDescriptor>();
    private ConcurrentDictionary<Type, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new();
    private ConcurrentDictionary<(string Type, int Version), ActivityDescriptor> _activityDescriptors = new();

    /// <inheritdoc />
    public void Add(Type providerType, ActivityDescriptor descriptor) => Add(descriptor, GetOrCreateDescriptors(providerType));

    /// <inheritdoc />
    public void Remove(Type providerType, ActivityDescriptor descriptor)
    {
        _providedActivityDescriptors[providerType].Remove(descriptor);
        _activityDescriptors.Remove((descriptor.TypeName, descriptor.Version), out _);
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
    public async Task RegisterAsync([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type activityType, CancellationToken cancellationToken)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);

        if (_activityDescriptors.Values.Any(x => x.TypeName == activityTypeName))
            return;

        var activityDescriptor = await activityDescriber.DescribeActivityAsync(activityType, cancellationToken);
        Add(GetType(), activityDescriptor);
        _manualActivityDescriptors.Add(activityDescriptor);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(IEnumerable<Type> activityTypes, CancellationToken cancellationToken = default)
    {
        foreach (var activityType in activityTypes)
            await RegisterAsync(activityType, cancellationToken);
    }
    
    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default) => new(_manualActivityDescriptors);

    /// <inheritdoc />
    public async Task RefreshDescriptorsAsync(IEnumerable<IActivityProvider> activityProviders, CancellationToken cancellationToken = default)
    {
        var providersDictionary = new ConcurrentDictionary<Type, ICollection<ActivityDescriptor>>();
        var activityDescriptors = new ConcurrentDictionary<(string Type, int Version), ActivityDescriptor>();
        foreach (var activityProvider in activityProviders)
        {
            var descriptors = (await activityProvider.GetDescriptorsAsync(cancellationToken)).ToList();
            var providerDescriptors = new List<ActivityDescriptor>();
            providersDictionary[activityProvider.GetType()] = providerDescriptors;
            foreach (var descriptor in descriptors)
            {
                Add(descriptor, activityDescriptors, providerDescriptors);
            }
        }
        
        Interlocked.Exchange(ref _activityDescriptors, activityDescriptors);
        Interlocked.Exchange(ref _providedActivityDescriptors, providersDictionary);
    }
    
    public async Task RefreshDescriptorsAsync(IActivityProvider activityProvider, CancellationToken cancellationToken = default)
    {
        var providersDictionary = new ConcurrentDictionary<Type, ICollection<ActivityDescriptor>>(_providedActivityDescriptors);
        var activityDescriptors = new ConcurrentDictionary<(string Type, int Version), ActivityDescriptor>(_activityDescriptors);
        var descriptors = (await activityProvider.GetDescriptorsAsync(cancellationToken)).ToList();
        var providerDescriptors = new List<ActivityDescriptor>();
        providersDictionary[activityProvider.GetType()] = providerDescriptors;
        
        foreach (var descriptor in descriptors) 
            Add(descriptor, activityDescriptors, providerDescriptors);
        
        Interlocked.Exchange(ref _activityDescriptors, activityDescriptors);
        Interlocked.Exchange(ref _providedActivityDescriptors, providersDictionary);
    }
    
    private void Add(ActivityDescriptor descriptor, ICollection<ActivityDescriptor> target)
    {
        Add(descriptor, _activityDescriptors, target);
    }

    private void Add(ActivityDescriptor? descriptor, ConcurrentDictionary<(string Type, int Version), ActivityDescriptor> activityDescriptors, ICollection<ActivityDescriptor> providerDescriptors)
    {
        if (descriptor is null)
        {
            logger.LogError("Unable to add a null descriptor");
            return;
        }
        
        foreach (var modifier in modifiers)
            modifier.Modify(descriptor);

        // If the descriptor already exists, replace it. But log a warning.
        if (activityDescriptors.TryGetValue((descriptor.TypeName, descriptor.Version), out var existingDescriptor))
        {
            // Remove the existing descriptor from the providerDescriptors collection.
            providerDescriptors.Remove(existingDescriptor);

            // Log a warning.
            logger.LogWarning("Activity descriptor {ActivityType} v{ActivityVersion} was already registered. Replacing with new descriptor", descriptor.TypeName, descriptor.Version);
        }

        activityDescriptors[(descriptor.TypeName, descriptor.Version)] = descriptor;
        providerDescriptors.Add(descriptor);
    }

    private ICollection<ActivityDescriptor> GetOrCreateDescriptors(Type provider)
    {
        if (_providedActivityDescriptors.TryGetValue(provider, out var descriptors))
            return descriptors;

        descriptors = new List<ActivityDescriptor>();
        _providedActivityDescriptors[provider]= descriptors;

        return descriptors;
    }
}