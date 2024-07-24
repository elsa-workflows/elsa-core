namespace Elsa.Features.Attributes;

/// <summary>
/// Specifies that the feature depends on another feature.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
/// </remarks>
/// <param name="type">The type of the feature this feature depends on.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute(Type type) : Attribute
{

    /// <summary>
    /// The type of the feature this feature depends on.
    /// </summary>
    public Type Type { get; set; } = type;
}