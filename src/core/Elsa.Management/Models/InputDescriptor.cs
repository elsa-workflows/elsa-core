namespace Elsa.Management.Models;

public class InputDescriptor : PropertyDescriptor
{
    public InputDescriptor()
    {
    }

    public InputDescriptor(
        string name,
        Type type,
        string uiHint,
        string displayName,
        string? description = default,
        object? options = default,
        string? category = default,
        float order = 0,
        object? defaultValue = default,
        string? defaultSyntax = "Literal",
        IEnumerable<string>? supportedSyntaxes = default,
        bool isReadOnly = false,
        bool isBrowsable = true)
    {
        Name = name;
        Type = type;
        UIHint = uiHint;
        DisplayName = displayName;
        Description = description;
        Options = options;
        Category = category;
        Order = order;
        DefaultValue = defaultValue;
        DefaultSyntax = defaultSyntax;
        SupportedSyntaxes = supportedSyntaxes?.ToList() ?? new List<string>();
        IsReadOnly = isReadOnly;
        IsBrowsable = isBrowsable;
    }

    public string UIHint { get; set; } = default!;
    public object? Options { get; set; }
    public string? Category { get; set; }
    public object? DefaultValue { get; set; }
    public string? DefaultSyntax { get; set; }
    public ICollection<string> SupportedSyntaxes { get; set; } = new List<string>();
    public bool? IsReadOnly { get; set; }
}