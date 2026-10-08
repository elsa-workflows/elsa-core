namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a feature that can be registered with the dashboard.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// Called by the dashboard to initialize the feature.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}