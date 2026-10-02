using System.Reflection;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultFeatureService : IFeatureService
{
    private readonly IEnumerable<IFeature> _features;
    private readonly IRemoteFeatureProvider _remoteFeatureProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultFeatureService"/> class.
    /// </summary>
    public DefaultFeatureService(IEnumerable<IFeature> features, IRemoteFeatureProvider remoteFeatureProvider)
    {
        _features = features;
        _remoteFeatureProvider = remoteFeatureProvider;
    }
    
    /// <inheritdoc />
    public event Action? Initialized;

    /// <inheritdoc />
    public IEnumerable<IFeature> GetFeatures()
    {
        return _features.ToList();
    }

    /// <inheritdoc />
    public async Task InitializeFeaturesAsync(CancellationToken cancellationToken = default)
    {
        var remoteFeatures = (await _remoteFeatureProvider.ListAsync(cancellationToken)).ToList();

        foreach (var feature in GetFeatures())
        {
            var remoteFeatureName = feature.GetType().GetCustomAttribute<RemoteFeatureAttribute>()?.Name;

            if (!string.IsNullOrWhiteSpace(remoteFeatureName))
            {
                // Check if the remote feature is enabled.
                var remoteFeatureIsEnabled = remoteFeatures.Any(x => x.FullName == remoteFeatureName);

                if (!remoteFeatureIsEnabled)
                    continue;
            }

            await feature.InitializeAsync(cancellationToken);
        }
        
        OnInitialized();
    }

    private void OnInitialized()
    {
        Initialized?.Invoke();
    }
}