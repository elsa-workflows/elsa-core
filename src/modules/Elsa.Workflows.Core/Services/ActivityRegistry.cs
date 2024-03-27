using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Workflows.Services;

/// <inheritdoc />
public class ActivityRegistry : IActivityRegistry
{
    private readonly ReaderWriterLockSlim _manualActivityLock = new();
    private readonly ReaderWriterLockSlim _providedActivityLock = new();
    private readonly ReaderWriterLockSlim _activityLock = new(LockRecursionPolicy.SupportsRecursion);
    private readonly IActivityDescriber _activityDescriber;
    private readonly IEnumerable<IActivityDescriptorModifier> _modifiers;
    private readonly ILogger _logger;
    private readonly ISet<ActivityDescriptor> _manualActivityDescriptors = new HashSet<ActivityDescriptor>();
    private readonly IDictionary<Type, ICollection<ActivityDescriptor>> _providedActivityDescriptors = new Dictionary<Type, ICollection<ActivityDescriptor>>();
    private readonly IDictionary<(string Type, int Version), ActivityDescriptor> _activityDescriptors = new Dictionary<(string Type, int Version), ActivityDescriptor>();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActivityRegistry"/> class.
    /// </summary>
    public ActivityRegistry(IActivityDescriber activityDescriber, IEnumerable<IActivityDescriptorModifier> modifiers, ILogger<ActivityRegistry> logger)
    {
        _activityDescriber = activityDescriber;
        _modifiers = modifiers;
        _logger = logger;
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
        if (_activityLock.TryEnterWriteLock(250)
            && _providedActivityLock.TryEnterWriteLock(250))
        {
            try
            {
                _activityDescriptors.Clear();
                _providedActivityDescriptors.Clear();
            }
            finally
            {
                _activityLock.ExitWriteLock();
                _providedActivityLock.ExitWriteLock();
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire write locks for provided and activity descriptors.");
        }
    }

    /// <inheritdoc />
    public void ClearProvider(Type providerType)
    {
        var descriptors = ListByProvider(providerType).ToList();

        if (_activityLock.TryEnterWriteLock(250)
            && _providedActivityLock.TryEnterWriteLock(250))
        {
            try
            {
                foreach (var descriptor in descriptors)
                    _activityDescriptors.Remove((descriptor.TypeName, descriptor.Version), out _);

                _providedActivityDescriptors.Remove(providerType);
            }
            finally
            {
                _activityLock.ExitWriteLock();
                _providedActivityLock.ExitWriteLock();
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire write locks for provided and activity descriptors.");
        }
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListAll()
    {
        if (_activityLock.TryEnterReadLock(250))
        {
            try
            {
                return _activityDescriptors.Values;
            }
            finally
            {
                _activityLock.ExitReadLock();
            }
        }

        _logger.LogWarning("Failed to acquire read lock for activity descriptors.");
        return Enumerable.Empty<ActivityDescriptor>();

    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> ListByProvider(Type providerType)
    {
        if (_providedActivityLock.TryEnterReadLock(250))
        {
            try
            {
                return _providedActivityDescriptors.TryGetValue(providerType, out var descriptors)
                    ? descriptors
                    : ArraySegment<ActivityDescriptor>.Empty;
            }
            finally
            {
                _providedActivityLock.ExitReadLock();
            }
        }

        _logger.LogWarning("Failed to acquire read lock for provided activity descriptors.");
        return Enumerable.Empty<ActivityDescriptor>();
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type)
    {
        if (_activityLock.TryEnterReadLock(250))
        {
            try
            {
                return _activityDescriptors.Values.Where(x => x.TypeName == type).MaxBy(x => x.Version);
            }
            finally
            {
                _activityLock.ExitReadLock();
            }
        }
        
        _logger.LogWarning("Failed to acquire read lock for activity descriptors.");
        return null;
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(string type, int version)
    {
        if (_activityLock.TryEnterReadLock(250))
        {
            try
            {
                return _activityDescriptors.TryGetValue((type, version), out var descriptor)
                    ? descriptor
                    : null;
            }
            finally
            {
                _activityLock.ExitReadLock();
            }
        }
        
        _logger.LogWarning("Failed to acquire read lock for activity descriptors.");
        return null;
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(Func<ActivityDescriptor, bool> predicate)
    {
        if (_activityLock.TryEnterReadLock(250))
        {
            try
            {
                return _activityDescriptors.Values.FirstOrDefault(predicate);
            }
            finally
            {
                _activityLock.ExitReadLock();
            }
        }
        
        _logger.LogWarning("Failed to acquire read lock for activity descriptors.");
        return null;
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindMany(Func<ActivityDescriptor, bool> predicate)
    {
        if (_activityLock.TryEnterReadLock(250))
        {
            try
            {
                return _activityDescriptors.Values.Where(predicate);
            }
            finally
            {
                _activityLock.ExitReadLock();
            }
        }
        
        _logger.LogWarning("Failed to acquire read lock for activity descriptors.");
        return Enumerable.Empty<ActivityDescriptor>();
    }

    /// <inheritdoc />
    public void Register(ActivityDescriptor descriptor)
    {
        Add(GetType(), descriptor);
    }

    /// <inheritdoc />
    public async Task RegisterAsync(Type activityType, CancellationToken cancellationToken)
    {
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName(activityType);

        if (_activityLock.TryEnterUpgradeableReadLock(250))
        {
            try
            {
                if (_activityDescriptors.Values.Any(x => x.TypeName == activityTypeName))
                    return;

                if (_manualActivityLock.TryEnterWriteLock(250))
                {
                    try
                    {
                        var activityDescriptor = await _activityDescriber.DescribeActivityAsync(activityType, cancellationToken);
                        Add(GetType(), activityDescriptor);
                        _manualActivityDescriptors.Add(activityDescriptor);
                    }
                    finally
                    {
                        _manualActivityLock.ExitWriteLock();
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to acquire write lock for manual activity descriptors.");
                }
            }
            finally
            {
                _activityLock.ExitUpgradeableReadLock();
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire upgradable lock for activity descriptors.");
        }
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

        if (_activityLock.TryEnterUpgradeableReadLock(250))
        {
            try
            {
                // If the descriptor already exists, replace it. But log a warning.
                if (_activityDescriptors.TryGetValue((descriptor.TypeName, descriptor.Version),
                        out var existingDescriptor))
                {
                    // Remove the existing descriptor from the target collection.
                    target.Remove(existingDescriptor);

                    // Log a warning.
                    _logger.LogWarning(
                        "Activity descriptor {ActivityType} v{ActivityVersion} was already registered. Replacing with new descriptor",
                        descriptor.TypeName, descriptor.Version);
                }

                if (_activityLock.TryEnterWriteLock(250))
                {
                    try
                    {
                        _activityDescriptors[(descriptor.TypeName, descriptor.Version)] = descriptor;
                        target.Add(descriptor);
                    }
                    finally
                    {
                        _activityLock.ExitWriteLock();
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to acquire write lock for activity descriptors.");
                }
            }
            finally
            {
                _activityLock.ExitUpgradeableReadLock();
            }
        }
        else
        {
            _logger.LogWarning("Failed to acquire upgradable lock for activity descriptors.");
        }
    }

    private ICollection<ActivityDescriptor> GetOrCreateDescriptors(Type provider)
    {
        if (_providedActivityLock.TryEnterUpgradeableReadLock(250))
        {
            try
            {
                if (_providedActivityDescriptors.TryGetValue(provider, out var descriptors))
                    return descriptors;

                descriptors = new List<ActivityDescriptor>();

                if (_providedActivityLock.TryEnterWriteLock(250))
                {
                    try
                    {
                        _providedActivityDescriptors.Add(provider, descriptors);
                    }
                    finally
                    {
                        _providedActivityLock.ExitWriteLock();
                    }
                }
                else
                {
                    _logger.LogWarning("Failed to acquire write lock for provided activity descriptors.");
                }

                return descriptors;
            }
            finally
            {
                _providedActivityLock.ExitUpgradeableReadLock();
            }
        }
        
        _logger.LogWarning("Failed to acquire upgradable lock for provided activity descriptors.");
        return Enumerable.Empty<ActivityDescriptor>().ToList();
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<ActivityDescriptor>> GetDescriptorsAsync(CancellationToken cancellationToken = default)
    {
        if (_manualActivityLock.TryEnterReadLock(250))
        {
            try
            {
                return new(_manualActivityDescriptors);
            }
            finally
            {
                _manualActivityLock.ExitReadLock();
            }
        }
        _logger.LogWarning("Failed to acquire read lock for manual activity descriptors.");
        return ValueTask.FromResult(Enumerable.Empty<ActivityDescriptor>());
    }
}