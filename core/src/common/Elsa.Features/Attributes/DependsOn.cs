namespace Elsa.Features.Attributes;

/// <summary>
/// Specifies that the feature depends on another feature.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DependsOnAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependsOnAttribute"/> class.
    /// </summary>
    /// <param name="type">The type of the feature this feature depends on.</param>
    public DependsOnAttribute(Type type)
    {
        Type = type;
    }
    
    /// <summary>
    /// The type of the feature this feature depends on.
    /// </summary>
    public Type Type { get; set; }
}