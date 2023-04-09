using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Core.Models;

public class OutputDescriptor : PropertyDescriptor
{
    public OutputDescriptor()
    {
    }

    public OutputDescriptor(
        string name,
        string displayName,
        Type type,
        Func<IActivity, object?> valueGetter,
        string? description = default,
        bool? isBrowsable = default)
    {
        Name = name;
        DisplayName = displayName;
        Type = type;
        ValueGetter = valueGetter;
        Description = description;
        IsBrowsable = isBrowsable;
    }
}