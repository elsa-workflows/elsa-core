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
    /// Whether the features have been initialized, so everything they contribute (such as dashboard widgets) is in place.
    /// Lets a component that renders after <see cref="Initialized"/> was raised tell that apart from one that has yet to
    /// see it. A service that decorates another should forward it.
    /// </summary>
    bool IsInitialized => false;

    /// <summary>
    /// Returns all features.
    /// </summary>
    IEnumerable<IFeature> GetFeatures();

    /// <summary>
    /// Initializes all features.
    /// </summary>
    Task InitializeFeaturesAsync(CancellationToken cancellationToken = default);
}