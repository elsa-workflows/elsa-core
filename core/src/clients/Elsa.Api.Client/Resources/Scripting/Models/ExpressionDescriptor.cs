using System.Text.Json.Serialization;

namespace Elsa.Api.Client.Resources.Scripting.Models;

/// <summary>
/// Represents a descriptor for an expression.
/// </summary>
public class ExpressionDescriptor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionDescriptor"/> class.
    /// </summary>
    [JsonConstructor]
    public ExpressionDescriptor()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpressionDescriptor"/> class.
    /// </summary>
    /// <param name="type">The expression type.</param>
    /// <param name="displayName">The display name of the expression type.</param>
    public ExpressionDescriptor(string type, string displayName)
    {
        Type = type;
        DisplayName = displayName;
    }
    
    /// <summary>
    /// Gets or sets the expression type.
    /// </summary>
    public string Type { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets the display name of the expression type.
    /// </summary>
    public string DisplayName { get; set; } = default!;
    
    /// <summary>
    /// Gets or sets whether the expression value is serializable.
    /// </summary>
    public bool IsSerializable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the expression type is browsable.
    /// </summary>
    public bool IsBrowsable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the expression type properties.
    /// </summary>
    public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
}