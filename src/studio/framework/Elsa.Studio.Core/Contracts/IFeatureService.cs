namespace Elsa.Studio.Contracts;

/// <summary>
/// Manages features.
/// </summary>
public interface IFeatureService
{
    /// <summary>
    /// Event that is triggered when the features have been initialized.
    /// </summary>
    event Action? Initialized;

    /// <summary>
    /// Whether <see cref="InitializeFeaturesAsync"/> has completed.
    /// </summary>
    bool IsInitialized { get; }
    
    /// <summary>
    /// Returns all features.
    /// </summary>
    IEnumerable<IFeature> GetFeatures();

    /// <summary>
    /// Initializes all features.
    /// </summary>
    Task InitializeFeaturesAsync(CancellationToken cancellationToken = default);
}