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
    /// <remarks>
    /// Default is <c>false</c> so third-party implementations and decorators compiled against
    /// earlier Studio packages keep working. Making this member abstract would be a binary break.
    /// </remarks>
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