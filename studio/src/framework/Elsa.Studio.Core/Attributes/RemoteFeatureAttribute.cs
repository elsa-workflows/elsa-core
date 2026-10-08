namespace Elsa.Studio.Attributes;

/// <summary>
/// Marks a class as a remote feature.
/// </summary>
public class RemoteFeatureAttribute : Attribute
{
    /// <inheritdoc />
    public RemoteFeatureAttribute(string name)
    {
        Name = name;
    }
    
    /// <summary>
    /// Gets the name of the remote feature.
    /// </summary>
    public string Name { get; }
}