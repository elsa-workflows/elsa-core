namespace Elsa.Features.Services;

/// <summary>
/// Represents a feature.
/// </summary>
public interface IFeature
{
    /// <summary>
    /// Gets the module that this feature belongs to.
    /// </summary>
    IModule Module { get; }
    
    /// <summary>
    /// Configures the feature.
    /// </summary>
    void Configure();
    
    /// <summary>
    /// Configures the hosted services.
    /// </summary>
    void ConfigureHostedServices();
    
    /// <summary>
    /// Applies the feature by adding services to the service collection.
    /// </summary>
    void Apply();
    
}