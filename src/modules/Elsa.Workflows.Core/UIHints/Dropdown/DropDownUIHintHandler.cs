using System.Reflection;
using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.UIHints.Dropdown;

/// <inheritdoc />
public class DropDownUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => InputUIHints.DropDown;

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new(new[] { typeof(StaticDropDownOptionsProvider) });
    }
}