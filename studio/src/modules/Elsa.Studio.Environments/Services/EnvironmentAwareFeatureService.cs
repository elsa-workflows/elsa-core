using Elsa.Studio.Contracts;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// Selects the default environment once, then initializes features against that backend.
/// </summary>
internal sealed class EnvironmentAwareFeatureService(
    IFeatureService inner,
    EnvironmentLoader environmentLoader) : IFeatureService
{
    /// <inheritdoc />
    public event Action? Initialized
    {
        add => inner.Initialized += value;
        remove => inner.Initialized -= value;
    }

    /// <inheritdoc />
    public bool IsInitialized => inner.IsInitialized;

    /// <inheritdoc />
    public IEnumerable<IFeature> GetFeatures() => inner.GetFeatures();

    /// <inheritdoc />
    public async Task InitializeFeaturesAsync(CancellationToken cancellationToken = default)
    {
        await environmentLoader.EnsureLoadedAsync(cancellationToken);
        await inner.InitializeFeaturesAsync(cancellationToken);
    }
}
