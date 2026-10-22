namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides an uncached installed-feature check for callers whose authorization depends on current backend context.
/// </summary>
public interface IRemoteFeatureProviderWithFreshCheck
{
    /// <summary>
    /// Checks a feature against the backend's current installed-feature catalog without reusing a cached catalog.
    /// </summary>
    Task<bool> IsEnabledFreshAsync(string featureName, CancellationToken cancellationToken = default);
}
