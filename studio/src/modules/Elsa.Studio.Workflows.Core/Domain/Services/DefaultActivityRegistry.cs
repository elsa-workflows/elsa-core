using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class DefaultActivityRegistry : IActivityRegistry
{
    private readonly IActivityRegistryProvider _provider;
    private readonly IMediator _mediator;
    private Dictionary<(string ActivityTypeName, int Version), ActivityDescriptor> _activityDescriptors = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _isStale = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityRegistry"/> class.
    /// </summary>
    public DefaultActivityRegistry(IActivityRegistryProvider provider, IMediator mediator)
    {
        _provider = provider;
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = await _provider.ListAsync(cancellationToken);
        _activityDescriptors = descriptors.ToDictionary(x => (x.TypeName, x.Version));
        _isStale = false;
        await _mediator.NotifyAsync(new ActivityRegistryRefreshed(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        
        try
        {
            if (!_isStale)
                return;
            
            await RefreshAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> List()
    {
        // Return the latest version of each activity descriptor from _activityDescriptors.
        return _activityDescriptors.Values
            .GroupBy(activityDescriptor => activityDescriptor.TypeName)
            .Select(grouping => grouping.OrderByDescending(y => y.Version).First());
    }

    /// <inheritdoc />
    public ActivityDescriptor? Find(string activityType, int? version = default)
    {
        version ??= 1;
        return _activityDescriptors.GetValueOrDefault((activityType, version.Value));
    }
    
    /// <inheritdoc />
    public IEnumerable<ActivityDescriptor> FindAll(string activityType)
    {
        return _activityDescriptors
            .Where(kvp => kvp.Key.ActivityTypeName == activityType)
            .Select(kvp => kvp.Value);
    }

    /// <inheritdoc />
    public void MarkStale()
    {
        _isStale = true;
    }
}