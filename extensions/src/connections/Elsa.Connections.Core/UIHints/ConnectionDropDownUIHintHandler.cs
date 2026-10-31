using System.Reflection;
using Elsa.Workflows;
using Elsa.Workflows.UIHints;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Connections.UIHints;

/// <summary>
/// UIHint Handler that use DropDown for Connection Property
/// </summary>
public class ConnectionDropDownUIHintHandler : IUIHintHandler
{
    /// <inheritdoc />
    public string UIHint => $"connection-{InputUIHints.DropDown}";

    /// <inheritdoc />
    public ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken)
    {
        return new([typeof(StaticDropDownOptionsProvider)]);
    }
}