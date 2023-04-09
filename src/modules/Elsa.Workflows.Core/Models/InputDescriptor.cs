using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

/// <summary>
/// A descriptor of an activity's input property.
/// </summary>
public class InputDescriptor : PropertyDescriptor
{
    /// <inheritdoc />
    public InputDescriptor()
    {
    }

    /// <inheritdoc />
    public InputDescriptor(
        string name,
        Type type,
        Func<IActivity, object?> valueGetter,
        bool isWrapped,
        string uiHint,
        string displayName,
        string? description = default,
        object? options = default,
        string? category = default,
        float order = 0,
        object? defaultValue = default,
        string? defaultSyntax = "Literal",
        //IEnumerable<string>? supportedSyntaxes = default,
        bool isReadOnly = false,
        bool isBrowsable = true,
        bool isSynthetic = false,
        Type? storageDriverType = default)
    {
        Name = name;
        Type = type;
        ValueGetter = valueGetter;
        IsWrapped = isWrapped;
        UIHint = uiHint;
        DisplayName = displayName;
        Description = description;
        Options = options;
        Category = category;
        Order = order;
        DefaultValue = defaultValue;
        DefaultSyntax = defaultSyntax;
        //SupportedSyntaxes = supportedSyntaxes?.ToList() ?? new List<string>();
        IsReadOnly = isReadOnly;
        StorageDriverType = storageDriverType;
        IsSynthetic = isSynthetic;
        IsBrowsable = isBrowsable;
    }

    /// <summary>
    /// True if the property is wrapped with an <see cref="Input{T}"/> type, false otherwise.
    /// </summary>
    public bool IsWrapped { get; set; }
    /// <summary>
    /// A string value that hints at what UI control might be used to render in a UI tool.  
    /// </summary>
    public string UIHint { get; set; } = default!;
    
    /// <summary>
    /// An arbitrary options object that can be used by some UI tool.
    /// </summary>
    public object? Options { get; set; }
    
    /// <summary>
    /// The category to whcih this input belongs. Can be used by UI to e.g. render different inputs in different tabs.
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
    public Type? StorageDriverType { get; set; }
}