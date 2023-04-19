using JetBrains.Annotations;

namespace Elsa.Features.Models;

/// <summary>
/// Represents a feature descriptor.
/// </summary>
[PublicAPI]
public class FeatureDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDescriptor"/> class.
    /// </summary>
    public FeatureDescriptor()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureDescriptor"/> class.
    /// </summary>
    /// <param name="name">The name of the feature.</param>
    /// <param name="displayName">The display name for the feature.</param>
    /// <param name="description">The description of the feature.</param>
    public FeatureDescriptor(string name, string displayName, string? description = default)
    {
        Name = name;
        DisplayName = displayName;
        Description = description ?? "";
    }

    /// <summary>
    /// The name of the feature.
    /// </summary>
    public string Name { get; set; } = default!;

    /// <summary>
    /// The display name for the feature.
    /// </summary>
    public string DisplayName { get; set; } = default!;
    
    /// <summary>
    /// The description of the feature.
    /// </summary>
    public string Description { get; set; } = "";
}