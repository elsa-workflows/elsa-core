using JetBrains.Annotations;

namespace Elsa.Api.Client.Resources.ActivityDescriptors.Models;

/// <summary>
/// A descriptor of an activity's input property.
/// </summary>
[PublicAPI]
public class InputDescriptor : PropertyDescriptor
{
    /// <inheritdoc />
    public InputDescriptor()
    {
    }
    
    /// <summary>
    /// True if the property is wrapped with an <c>"Input{T}"</c> type, false otherwise.
    /// </summary>
    public bool IsWrapped { get; set; }
    
    /// <summary>
    /// A string value that hints at what UI control might be used to render in a UI tool.  
    /// </summary>
    public string UIHint { get; set; } = default!;
    
    /// <summary>
    /// An arbitrary options object that can be used by UI tooling.
    /// </summary>
    public IDictionary<string, object>? Options { get; set; }
    
    /// <summary>
    /// The category to which this input belongs. Can be used by UI to e.g. render different inputs in different tabs.
    /// </summary>
    public string? Category { get; set; }
    
    /// <summary>
    /// The default value.
    /// </summary>
    public object? DefaultValue { get; set; }
    
    /// <summary>
    /// The default syntax.
    /// </summary>
    public string? DefaultSyntax { get; set; }
    //public ICollection<string> SupportedSyntaxes { get; set; } = new List<string>();
    
    /// <summary>
    /// True if the input is readonly, false otherwise.
    /// </summary>
    public bool? IsReadOnly { get; set; }
    
    /// <summary>
    /// The storage driver type to use for persistence.
    /// If no driver is specified, the referenced memory block will remain in memory for as long as the expression execution context exists.
    /// </summary>
    public string? StorageDriverType { get; set; }
}