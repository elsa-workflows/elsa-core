namespace Elsa.Workflows.Management.Models;

public class OutputDescriptor : PropertyDescriptor
{
    public OutputDescriptor()
    {
    }

    public OutputDescriptor(
        string name,
        string displayName,
        Type type,
        string? description = default,
        bool? isBrowsable = default)
    {
        Name = name;
        DisplayName = displayName;
        Type = type;
        Description = description;
        IsBrowsable = isBrowsable;
    }
}