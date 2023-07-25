using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.Features.Models;

/// <summary>
/// Represents a feature descriptor.
/// </summary>
[PublicAPI]
public class FeatureDescriptor
{
    /// <summary>
    /// Gets or sets the name of the feature.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// Gets or sets the namespace of the feature.
    /// </summary>
    public string Namespace { get; set; } = default!;

    /// <summary>
    /// Gets the full name of the feature.
    /// </summary>
    public string FullName { get; set; } = default!;

    /// <summary>
    /// The display name for the feature.
    /// </summary>
    public string DisplayName { get; set; } = default!;
    
    /// <summary>
    /// The description of the feature.
    /// </summary>
    public string Description { get; set; } = "";
}