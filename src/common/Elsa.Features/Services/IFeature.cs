using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Elsa.Features.Services;

/// <summary>
/// Represents a feature.
/// </summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors | ImplicitUseTargetFlags.Members)]
public interface IFeature
{
    /// <summary>
    /// Gets the module that this feature belongs to.
    /// </summary>
    IModule Module { get; }
    
    /// <summary>
    /// Configures the feature.
    /// </summary>
    [RequiresUnreferencedCode("The assembly containing the specified marker type will be scanned for activity types.")]
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