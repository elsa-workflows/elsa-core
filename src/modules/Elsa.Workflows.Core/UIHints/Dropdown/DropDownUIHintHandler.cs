using System.Reflection;

namespace Elsa.Workflows.UIHints.Dropdown;

/// <inheritdoc />
public class DropDownUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.DropDown;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new([typeof(StaticDropDownOptionsProvider)]);
    }
}