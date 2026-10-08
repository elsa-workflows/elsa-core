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
    /// <param name="ns">The namespace of the feature.</param>
    /// <param name="displayName">The display name for the feature.</param>
    /// <param name="description">The description of the feature.</param>
    public FeatureDescriptor(string name, string ns, string displayName, string? description = default)
    {
        Name = name;
        Namespace = ns;
        DisplayName = displayName;
        Description = description ?? "";
    }

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
    public string FullName => $"{Namespace}.{Name}";

    /// <summary>
    /// The display name for the feature.
    /// </summary>
    public string DisplayName { get; set; } = default!;
    
    /// <summary>
    /// The description of the feature.
    /// </summary>
    public string Description { get; set; } = "";
}